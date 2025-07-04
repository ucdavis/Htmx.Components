using System;

namespace Htmx.Components.AuthStatus;

/// <summary>
/// Marks action methods that should trigger an update of the authentication status component
/// when executed via HTMX requests. This attribute is used by the <see cref="Filters.AuthStatusUpdateFilter"/>
/// to automatically inject updated authentication status into out-of-band responses.
/// </summary>
/// <remarks>
/// This attribute only affects HTMX requests. For regular HTTP requests, the authentication status
/// component must be explicitly rendered in the view.
/// </remarks>
/// <example>
/// <code>
/// [HttpPost]
/// [AuthStatusUpdate]
/// public async Task&lt;IActionResult&gt; Login(LoginModel model)
/// {
///     // Login logic here
///     return Ok();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthStatusUpdateAttribute : Attribute
{
}