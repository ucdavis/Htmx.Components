using Htmx.Components.AuthStatus;
using Htmx.Components.Filters;
using Htmx.Components.Models;
using Htmx.Components.NavBar;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Htmx.Components.AuthStatus.Internal;

/// <summary>
/// A result filter that automatically updates authentication status and navigation components
/// when controller actions are marked with the <see cref="AuthStatusUpdateAttribute"/>.
/// This filter performs out-of-band updates for both the navigation bar and authentication status displays.
/// </summary>
/// <remarks>
/// This filter is designed specifically for HTMX requests and will throw an exception
/// if used with non-HTMX requests. For non-HTMX scenarios, use the <see cref="AuthStatusViewComponent"/> directly.
/// The filter automatically refreshes the navigation and authentication status to reflect
/// any changes that may have occurred during the request processing.
/// 
/// <para><strong>Example Pattern for Custom Component Filters:</strong></para>
/// <para>
/// This filter demonstrates the standard pattern for creating component-specific filters that
/// coordinate multiple out-of-band updates. When creating custom components that need coordinated
/// updates in response to specific events, follow this same pattern:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Create a custom filter that inherits from <see cref="OobResultFilterBase{T}"/> with your custom attribute</description>
/// </item>
/// <item>
/// <description>Keep the filter focused on a single responsibility (e.g., auth-related updates, data-related updates)</description>
/// </item>
/// <item>
/// <description>Override <see cref="UpdateMultiSwapViewResultAsync"/> to include only the related component updates</description>
/// </item>
/// <item>
/// <description>Register your filter in the MVC pipeline alongside existing filters</description>
/// </item>
/// </list>
/// <para>
/// Example: A separate filter for user profile-related components would be implemented as its own
/// filter class rather than modifying this authentication-focused filter, maintaining single responsibility.
/// </para>
/// </remarks>
public class AuthStatusUpdateFilter : OobResultFilterBase<AuthStatusUpdateAttribute>
{
    private readonly INavProvider _navProvider;
    private readonly IAuthStatusProvider _authStatusProvider;
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the AuthStatusUpdateFilter class.
    /// </summary>
    /// <param name="navProvider">The navigation provider for building updated navigation content.</param>
    /// <param name="authStatusProvider">The authentication status provider for building updated authentication status content.</param>
    /// <param name="viewPaths">The view paths configuration for component views.</param>
    public AuthStatusUpdateFilter(INavProvider navProvider, IAuthStatusProvider authStatusProvider, ViewPaths viewPaths)
    {
        _navProvider = navProvider;
        _authStatusProvider = authStatusProvider;
        _viewPaths = viewPaths;
    }

    /// <summary>
    /// Throws a NotSupportedException as this filter is designed only for HTMX requests.
    /// For non-HTMX scenarios, use the AuthStatusViewComponent directly in your views.
    /// </summary>
    /// <param name="attribute">The AuthStatusUpdateAttribute instance.</param>
    /// <param name="cad">The controller action descriptor.</param>
    /// <returns>This method always throws an exception.</returns>
    /// <exception cref="NotSupportedException">Always thrown as this filter doesn't support non-HTMX requests.</exception>
    protected override Task<string?> GetViewNameForNonHtmxRequest(AuthStatusUpdateAttribute attribute, ControllerActionDescriptor cad)
        => throw new NotSupportedException(
            $"{nameof(AuthStatusUpdateFilter)} does not support non-HTMX requests. Use {nameof(AuthStatusViewComponent)} instead.");

    /// <summary>
    /// Updates the MultiSwapViewResult with refreshed navigation bar and authentication status content.
    /// This method performs out-of-band updates for both components to ensure they reflect the current state.
    /// </summary>
    /// <param name="attribute">The AuthStatusUpdateAttribute that triggered this filter.</param>
    /// <param name="multiSwapViewResult">The MultiSwapViewResult to update with out-of-band content.</param>
    /// <param name="context">The result executing context containing request and user information.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    /// <remarks>
    /// <para><strong>Implementation Example for Component Developers:</strong></para>
    /// <para>
    /// This method demonstrates the standard pattern for implementing coordinated component updates
    /// in a filter. The pattern shown here should be followed when creating similar filters for
    /// other component groups, maintaining single responsibility within each filter.
    /// </para>
    /// <para>
    /// The implementation pattern demonstrated:
    /// 1. Retrieve updated data from appropriate providers (e.g., <see cref="INavProvider"/>, <see cref="IAuthStatusProvider"/>)
    /// 2. Use <c>multiSwapViewResult.WithOobContent()</c> to add each related component update
    /// 3. Target components using their well-known names from <see cref="ComponentNames"/>
    /// 4. Keep updates focused on the filter's single responsibility (authentication-related components)
    /// </para>
    /// </remarks>
    protected override async Task UpdateMultiSwapViewResultAsync(AuthStatusUpdateAttribute attribute, MultiSwapViewResult multiSwapViewResult, ResultExecutingContext context)
    {
        // Update NavBar
        var navbar = await _navProvider.BuildAsync();
        multiSwapViewResult.WithOobContent(ComponentNames.NavBar, navbar);

        // Update AuthStatus
        var user = context.HttpContext.User;
        var authStatus = await _authStatusProvider.GetAuthStatusAsync(user);
        multiSwapViewResult.WithOobContent(ComponentNames.AuthStatus, authStatus);
    }
}