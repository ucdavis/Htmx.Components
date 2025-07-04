using Htmx.Components.Filters;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Table;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Htmx.Components.Table.Internal;

/// <summary>
/// Internal result filter that processes table refresh actions to provide comprehensive
/// out-of-band updates for all table components. This filter is automatically applied
/// to controller actions marked with <see cref="TableRefreshActionAttribute"/>.
/// </summary>
/// <remarks>
/// This filter performs out-of-band updates for all major table components including
/// the table body, header, pagination, action list, and edit state. It's designed
/// exclusively for HTMX requests and will throw an exception if used with non-HTMX requests.
/// The filter ensures that all table-related UI elements are refreshed after operations
/// that may affect the table's data or state.
/// </remarks>
internal class TableOobRefreshFilter : OobResultFilterBase<TableRefreshActionAttribute>
{
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the TableOobRefreshFilter class.
    /// </summary>
    /// <param name="viewPaths">The view paths configuration for resolving table component views.</param>
    public TableOobRefreshFilter(ViewPaths viewPaths)
    {
        _viewPaths = viewPaths;
    }

    /// <summary>
    /// Updates the MultiSwapViewResult with comprehensive table component refreshes.
    /// This method adds out-of-band updates for all major table components to ensure
    /// the entire table UI reflects any changes that occurred during processing.
    /// </summary>
    /// <param name="attribute">The TableRefreshActionAttribute that triggered this filter.</param>
    /// <param name="multiSwapViewResult">The MultiSwapViewResult to update with table component content.</param>
    /// <param name="context">The result executing context.</param>
    /// <returns>A completed task as this operation is synchronous.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the MultiSwapViewResult has no model set.</exception>
    protected override Task UpdateMultiSwapViewResultAsync(TableRefreshActionAttribute attribute, MultiSwapViewResult multiSwapViewResult, ResultExecutingContext context)
    {
        if (multiSwapViewResult.Model == null)
        {
            throw new InvalidOperationException($"MultiSwapViewResult must have a model set when filtering via {nameof(TableRefreshActionAttribute)}.");
        }
        var tableModel = (ITableModel)multiSwapViewResult.Model;
        multiSwapViewResult
            .WithOobContent(_viewPaths.Table.TableActionList, tableModel)
            .WithOobContent(_viewPaths.Table.EditClassToggle, tableModel)
            .WithOobContent(_viewPaths.Table.Body, tableModel)
            .WithOobContent(_viewPaths.Table.Pagination, tableModel)
            .WithOobContent(_viewPaths.Table.Header, tableModel);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Throws an InvalidOperationException as this filter is designed exclusively for HTMX requests.
    /// Table refresh operations require HTMX's out-of-band update capabilities and cannot be
    /// handled through traditional full page renders.
    /// </summary>
    /// <param name="attribute">The TableRefreshActionAttribute instance.</param>
    /// <param name="cad">The controller action descriptor.</param>
    /// <returns>This method always throws an exception.</returns>
    /// <exception cref="InvalidOperationException">Always thrown as this filter doesn't support non-HTMX requests.</exception>
    protected override Task<string?> GetViewNameForNonHtmxRequest(TableRefreshActionAttribute attribute, ControllerActionDescriptor cad)
    {
        throw new InvalidOperationException($"{nameof(TableRefreshActionAttribute)} does not support non-HTMX requests.");
    }

}