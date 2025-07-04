using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Htmx.Components.Services;

/// <summary>
/// Provides services for extracting and evaluating authorization metadata from controller actions.
/// </summary>
/// <remarks>
/// This service caches authorization metadata and authorization results to improve performance
/// when checking user permissions for controller actions repeatedly.
/// </remarks>
public interface IAuthorizationMetadataService
{
    /// <summary>
    /// Extracts authorization metadata from a controller action descriptor.
    /// </summary>
    /// <param name="descriptor">The controller action descriptor to analyze.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the authorization metadata.</returns>
    Task<AuthorizationMetadata> GetMetadataAsync(ControllerActionDescriptor descriptor);
    
    /// <summary>
    /// Determines whether the specified user is authorized to access the controller action.
    /// </summary>
    /// <param name="descriptor">The controller action descriptor to check authorization for.</param>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the user is authorized; otherwise, false.</returns>
    Task<bool> IsAuthorizedAsync(ControllerActionDescriptor descriptor, ClaimsPrincipal user);
}

/// <summary>
/// Contains authorization metadata extracted from controller action attributes.
/// </summary>
public class AuthorizationMetadata
{
    /// <summary>
    /// Gets or sets the authorization policies required for the action.
    /// All policies must be satisfied for authorization to succeed.
    /// </summary>
    public string[] Policies { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the roles that are authorized to access the action.
    /// Any one of the specified roles is sufficient for authorization.
    /// </summary>
    public string[] Roles { get; set; } = [];
    
    /// <summary>
    /// Gets or sets a value indicating whether the action only requires authentication without specific policies or roles.
    /// </summary>
    public bool OnlyRequiresAuthentication { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether anonymous access is allowed for the action.
    /// When true, authorization checks are bypassed.
    /// </summary>
    public bool AllowAnonymous { get; set; }
}

/// <summary>
/// Configuration settings for the authorization metadata service.
/// </summary>
public class AuthorizationMetadataSettings
{
    /// <summary>
    /// Gets or sets the claim type used to identify the user ID for caching purposes.
    /// Defaults to <see cref="ClaimTypes.NameIdentifier"/>.
    /// </summary>
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;
}

/// <summary>
/// Implements authorization metadata extraction and evaluation with caching capabilities.
/// </summary>
public class AuthorizationMetadataService : IAuthorizationMetadataService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IMemoryCache _cache;
    private readonly AuthorizationMetadataSettings _settings;
    private readonly IRoleService? _roleService;
    private const int MetadataCacheExpirationMinutes = 10;
    private const int AuthorizationCacheExpirationMinutes = 2;

    /// <summary>
    /// Initializes a new instance of the AuthorizationMetadataService class.
    /// </summary>
    /// <param name="authorizationService">The authorization service for evaluating policies.</param>
    /// <param name="cache">The memory cache for storing authorization metadata and results.</param>
    /// <param name="settings">The configuration settings for the service.</param>
    /// <param name="roleService">The optional role service for role-based authorization checks.</param>
    public AuthorizationMetadataService(
        IAuthorizationService authorizationService,
        IMemoryCache cache,
        IOptions<AuthorizationMetadataSettings> settings,
        IRoleService? roleService = null)
    {
        _authorizationService = authorizationService;
        _cache = cache;
        _settings = settings.Value ?? new AuthorizationMetadataSettings();
        _roleService = roleService;
    }

    /// <summary>
    /// Extracts authorization metadata from a controller action descriptor with caching.
    /// </summary>
    /// <param name="descriptor">The controller action descriptor to analyze.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the authorization metadata.</returns>
    public async Task<AuthorizationMetadata> GetMetadataAsync(ControllerActionDescriptor descriptor)
    {
        // Extract and cache attribute metadata (not user-specific)
        var result = await _cache.GetOrCreateAsync(
            $"authmeta:{descriptor.UniqueId()}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(MetadataCacheExpirationMinutes);
                var authorizeAttrs = descriptor.MethodInfo
                    .GetCustomAttributes<AuthorizeAttribute>(true)
                    .Concat(descriptor.ControllerTypeInfo.GetCustomAttributes<AuthorizeAttribute>(true))
                    .ToArray();

                var policies = authorizeAttrs
                    .Where(attr => !string.IsNullOrEmpty(attr.Policy))
                    .Select(attr => attr.Policy!)
                    .Distinct()
                    .ToArray();

                var roles = authorizeAttrs
                    .Where(attr => !string.IsNullOrEmpty(attr.Roles))
                    .SelectMany(attr => attr.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct()
                    .ToArray();

                var onlyRequiresAuthentication = authorizeAttrs.Length > 0
                    && !policies.Any()
                    && !roles.Any();

                var allowAnonymous = descriptor.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>() != null
                    || descriptor.ControllerTypeInfo.GetCustomAttribute<AllowAnonymousAttribute>() != null;

                // Always return a non-null AuthorizationMetadata
                return Task.FromResult(new AuthorizationMetadata
                {
                    Policies = policies,
                    Roles = roles,
                    OnlyRequiresAuthentication = onlyRequiresAuthentication,
                    AllowAnonymous = allowAnonymous
                });
            });
        return result!;
    }

    /// <summary>
    /// Determines whether the specified user is authorized to access the controller action.
    /// Evaluates authentication requirements, authorization policies, and role-based permissions.
    /// </summary>
    /// <param name="descriptor">The controller action descriptor to check authorization for.</param>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the user is authorized; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when role-based authorization is required but no IRoleService is registered.</exception>
    public async Task<bool> IsAuthorizedAsync(ControllerActionDescriptor descriptor, ClaimsPrincipal user)
    {
        var meta = await GetMetadataAsync(descriptor);

        // AllowAnonymous always wins
        if (meta.AllowAnonymous)
            return true;

        // If there are no [Authorize] attributes at all, treat as public
        if (!meta.OnlyRequiresAuthentication && meta.Policies.Length == 0 && meta.Roles.Length == 0)
            return true;

        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

        // If only authentication is required
        if (meta.OnlyRequiresAuthentication)
            return isAuthenticated;

        // If not authenticated, fail fast (unless AllowAnonymous, already handled above)
        if (!isAuthenticated)
            return false;

        // Require ALL policies to succeed (AND semantics)
        foreach (var policy in meta.Policies)
        {
            var userId = user.FindFirst(_settings.UserIdClaimType)?.Value ?? "anonymous";
            var isAuthorized = await _cache.GetOrCreateAsync(
                $"authz:{userId}:{policy}",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(AuthorizationCacheExpirationMinutes);
                    var authResult = await _authorizationService.AuthorizeAsync(user, policy);
                    return authResult.Succeeded;
                });
            if (!isAuthorized)
                return false;
        }

        // Roles: OR semantics (any role is sufficient)
        if (meta.Roles.Length > 0)
        {
            if (_roleService == null)
                throw new InvalidOperationException("Role-based authorization is required, but no IRoleService is registered.");

            var hasRole = await _roleService.UserHasAnyRoleAsync(user, meta.Roles);
            if (!hasRole)
                return false;
        }

        // If we reach here, all requirements are satisfied
        return true;
    }
}

/// <summary>
/// Provides extension methods for controller action descriptors.
/// </summary>
public static class DescriptorExtensions
{
    /// <summary>
    /// Generates a unique identifier for a controller action descriptor for caching purposes.
    /// </summary>
    /// <param name="desc">The controller action descriptor.</param>
    /// <returns>A unique string identifier combining the controller type and action name.</returns>
    public static string UniqueId(this ControllerActionDescriptor desc)
        => $"{desc.ControllerTypeInfo.FullName}.{desc.ActionName}";
}