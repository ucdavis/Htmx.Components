using Microsoft.AspNetCore.Authorization;

namespace Htmx.Components.Authorization;

/// <summary>
/// Factory for creating authorization requirements based on roles or on resource and operation combinations.
/// </summary>
/// <remarks>
/// This interface allows applications to customize how authorization requirements are created
/// for different types of operations and resources. Implementations should integrate with
/// the application's authorization system to provide appropriate <see cref="IAuthorizationRequirement"/> instances.
/// </remarks>
/// <example>
/// <code>
/// public class CustomAuthorizationRequirementFactory : IAuthorizationRequirementFactory
/// {
///     public IAuthorizationRequirement ForOperation(string resource, string operation)
///     {
///         return new OperationAuthorizationRequirement { Name = $"{resource}:{operation}" };
///     }
///     
///     public IAuthorizationRequirement ForRoles(params string[] roles)
///     {
///         return new RolesAuthorizationRequirement(roles);
///     }
/// }
/// </code>
/// </example>
public interface IAuthorizationRequirementFactory
{
    /// <summary>
    /// Creates an authorization requirement for a specific resource and operation combination.
    /// </summary>
    /// <param name="resource">The resource being accessed (e.g., "users", "orders").</param>
    /// <param name="operation">The operation being performed (e.g., "read", "create", "update", "delete").</param>
    /// <returns>An <see cref="IAuthorizationRequirement"/> instance for the specified resource and operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resource"/> or <paramref name="operation"/> is null.</exception>
    IAuthorizationRequirement ForOperation(string resource, string operation);

    /// <summary>
    /// Creates an authorization requirement based on role membership.
    /// </summary>
    /// <param name="roles">The roles that are allowed to access the resource.</param>
    /// <returns>An <see cref="IAuthorizationRequirement"/> instance for the specified roles.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="roles"/> is null.</exception>
    IAuthorizationRequirement ForRoles(params string[] roles);
}
