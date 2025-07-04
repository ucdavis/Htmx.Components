namespace Htmx.Components.Authorization;

/// <summary>
/// Ensures that given resource and operation combinations are registered with the authorization system.
/// </summary>
/// <remarks>
/// This interface is used during model handler registration to ensure that all required
/// authorization policies or requirements are properly configured before they are needed
/// during runtime authorization checks.
/// </remarks>
/// <example>
/// <code>
/// public class DatabaseResourceOperationRegistry : IResourceOperationRegistry
/// {
///     public async Task Register(string resource, string operation)
///     {
///         // Ensure the resource-operation combination exists in database
///         await _context.ResourceOperations.AddIfNotExistsAsync(resource, operation);
///     }
/// }
/// </code>
/// </example>
public interface IResourceOperationRegistry
{
    /// <summary>
    /// Registers a resource and operation combination with the authorization system.
    /// </summary>
    /// <param name="resource">The resource being registered (e.g., "users", "orders").</param>
    /// <param name="operation">The operation being registered (e.g., "read", "create", "update", "delete").</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resource"/> or <paramref name="operation"/> is null.</exception>
    Task Register(string resource, string operation);
}
