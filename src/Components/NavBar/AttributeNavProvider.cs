using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Htmx.Components.NavBar;
using Htmx.Components.Models;
using Htmx.Components.Models.Builders;
using Microsoft.AspNetCore.Http;
using Htmx.Components.Services;
using Humanizer;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel;

namespace Htmx.Components.NavBar;

/// <summary>
/// Navigation provider that discovers navigation actions through <see cref="NavActionAttribute"/> 
/// and <see cref="NavActionGroupAttribute"/> attributes on controller actions.
/// </summary>
/// <remarks>
/// This provider automatically scans all controller actions for navigation attributes and
/// builds a navigation structure based on the discovered actions. Actions are filtered
/// based on the current user's authorization permissions before being included in the navigation.
/// </remarks>
/// <example>
/// To use attribute-based navigation, mark controller actions with navigation attributes:
/// <code>
/// [NavActionGroup(DisplayName = "User Management", Order = 10)]
/// public class UserController : Controller
/// {
///     [NavAction(DisplayName = "Users", Icon = "fas fa-users", Order = 1)]
///     public IActionResult Index() { ... }
///     
///     [NavAction(DisplayName = "Roles", Icon = "fas fa-shield", Order = 2)]
///     public IActionResult Roles() { ... }
/// }
/// </code>
/// </example>
public class AttributeNavProvider : INavProvider
{
    private readonly IActionDescriptorCollectionProvider _actions;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthorizationMetadataService _authMetadataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeNavProvider"/> class.
    /// </summary>
    /// <param name="actions">The action descriptor collection provider for discovering controller actions.</param>
    /// <param name="authorizationService">The authorization service for checking user permissions.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for accessing request context.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <param name="authMetadataService">The authorization metadata service for extracting authorization information.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AttributeNavProvider(
        IActionDescriptorCollectionProvider actions,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IAuthorizationMetadataService authMetadataService)
    {
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _authMetadataService = authMetadataService ?? throw new ArgumentNullException(nameof(authMetadataService));
    }

    /// <summary>
    /// Builds the navigation action set by discovering and filtering controller actions.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the navigation action set.</returns>
    public async Task<IActionSet> BuildAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            // If no user is available, return an empty ActionSet
            return new ActionSet(new ActionSetConfig());
        }
        var builder = new ActionSetBuilder(_serviceProvider);

        var descriptors = await Task.WhenAll(
            _actions.ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                .Where(desc => desc.MethodInfo.GetCustomAttribute<NavActionAttribute>() != null)
                .Select(async desc =>
                {
                    var meta = await _authMetadataService.GetMetadataAsync(desc);
                    return new NavActionDescriptor
                    {
                        MethodInfo = desc.MethodInfo,
                        ControllerTypeInfo = desc.ControllerTypeInfo,
                        ActionName = desc.ActionName,
                        ControllerName = desc.ControllerName,
                        ActionAttr = desc.MethodInfo.GetCustomAttribute<NavActionAttribute>()!,
                        GroupAttr = desc.MethodInfo.GetCustomAttribute<NavActionGroupAttribute>()
                            ?? desc.ControllerTypeInfo.GetCustomAttribute<NavActionGroupAttribute>(),
                        Metadata = meta,
                        Descriptor = desc
                    };
                })
        );
        var grouped = descriptors
        .GroupBy(x => (
            Order: x.GroupAttr != null ? x.GroupAttr.Order : x.ActionAttr.Order,
            DisplayNameAttribute: x.GroupAttr != null ? x.GroupAttr.DisplayName : x.ActionAttr.DisplayName,
            Icon: x.GroupAttr != null ? x.GroupAttr.Icon : x.ActionAttr.Icon
        ))
        .OrderBy(g => g.Key.Order);

        foreach (var group in grouped)
        {
            if (group.Count() == 1 && group.First().GroupAttr == null)
            {
                // If there's only one action and no group attribute, add it directly
                var desc = group.First();
                if (!await _authMetadataService.IsAuthorizedAsync(desc.Descriptor, user))
                    continue;

                builder.AddAction(BuildActionModelBuilder(desc));
                continue;
            }
            else
            {
                var actionModelBuilders = new List<Action<ActionModelBuilder>>();
                foreach (var desc in group)
                {
                    if (!await _authMetadataService.IsAuthorizedAsync(desc.Descriptor, user))
                        continue;
                    actionModelBuilders.Add(BuildActionModelBuilder(desc));
                }

                if (!actionModelBuilders.Any())
                    continue;

                builder.AddGroup(g =>
                {
                    var groupAttr = group.First().GroupAttr!;
                    g.WithLabel(groupAttr.DisplayName ?? groupAttr.DisplayName ?? group.First().ControllerName);
                    g.WithIcon(groupAttr.Icon ?? "");
                    foreach (var actionBuilder in actionModelBuilders)
                    {
                        g.AddAction(actionBuilder);
                    }
                });
            }
        }

        return await builder.BuildAsync();
    }

    private Action<ActionModelBuilder> BuildActionModelBuilder(NavActionDescriptor desc)
    {
        // Get current route info
        var httpContext = _httpContextAccessor.HttpContext;
        var routeData = httpContext?.GetRouteData();
        var currentController = routeData?.Values["controller"]?.ToString();
        var currentAction = routeData?.Values["action"]?.ToString();
        var isActive = string.Equals(desc.ControllerName, currentController, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(desc.ActionName, currentAction, StringComparison.OrdinalIgnoreCase);

        return a =>
        {
            a.WithIcon(desc.ActionAttr.Icon ?? "");
            a.WithIsActive(isActive);
            a.WithLabel(desc.ActionAttr.DisplayName ?? desc.ActionName.Humanize(LetterCasing.Title));
            // We're assuming an action named "Index" is the default action for the controller
            string url = desc.ActionName.Equals("Index", StringComparison.OrdinalIgnoreCase)
                ? $"/{desc.ControllerName}"
                : $"/{desc.ControllerName}/{desc.ActionName}";

            if (string.Equals(desc.ActionAttr.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                a.WithHxPost(url);
            else
                a.WithHxGet(url);
            a.WithHxPushUrl(desc.ActionAttr.PushUrl.ToString().ToLowerInvariant());
        };
    }

    /// <summary>
    /// Internal descriptor class used by the framework for navigation action metadata.
    /// Contains metadata about navigation items including method info, attributes, and authorization data.
    /// This class should not be used directly in user code.
    /// </summary>
    /// <remarks>
    /// This class is used internally by the navigation providers to store and transfer
    /// navigation metadata between different parts of the framework during the action discovery process.
    /// </remarks>
    private class NavActionDescriptor
    {
        public MethodInfo MethodInfo { get; set; } = default!;
        public TypeInfo ControllerTypeInfo { get; set; } = default!;
        public string ActionName { get; set; } = "";
        public string ControllerName { get; set; } = "";
        public NavActionAttribute ActionAttr { get; set; } = default!;
        public NavActionGroupAttribute? GroupAttr { get; set; }
        public object? Metadata { get; set; }
        public ControllerActionDescriptor Descriptor { get; set; } = default!;
    }
}