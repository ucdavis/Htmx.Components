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
/// Internal result filter that processes table edit actions to provide targeted
/// out-of-band updates for edited table rows and related components. This filter is
/// automatically applied to controller actions marked with <see cref="TableEditActionAttribute"/>.
/// </summary>
/// <remarks>
/// This filter performs selective out-of-band updates focusing on the specific rows that
/// have been edited, along with the table's action list and edit state toggle. Unlike
/// the refresh filter, this filter only updates the components that are directly affected
/// by edit operations, providing more efficient updates. It's designed exclusively for
/// HTMX requests and will throw an exception if used with non-HTMX requests.
/// </remarks>
public class TableOobEditFilter : OobResultFilterBase<TableEditActionAttribute>
{
    private readonly ITableProvider _tableProvider;
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the TableOobEditFilter class.
    /// </summary>
    /// <param name="tableProvider">The table provider for table-related operations.</param>
    /// <param name="viewPaths">The view paths configuration for resolving table component views.</param>
    public TableOobEditFilter(ITableProvider tableProvider, ViewPaths viewPaths)
    {
        _tableProvider = tableProvider;
        _viewPaths = viewPaths;
    }

    /// <summary>
    /// Updates the MultiSwapViewResult with targeted table row and component updates.
    /// This method adds out-of-band updates for the edit state toggle, action list,
    /// and individual table rows that have been modified during the edit operation.
    /// </summary>
    /// <param name="attribute">The TableEditActionAttribute that triggered this filter.</param>
    /// <param name="multiSwapViewResult">The MultiSwapViewResult to update with table component content.</param>
    /// <param name="context">The result executing context.</param>
    /// <returns>A completed task as this operation is synchronous.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the MultiSwapViewResult has no model set.</exception>
    protected override Task UpdateMultiSwapViewResultAsync(TableEditActionAttribute attribute, MultiSwapViewResult multiSwapViewResult, ResultExecutingContext context)
    {
        if (multiSwapViewResult.Model == null)
        {
            throw new InvalidOperationException($"MultiSwapViewResult must have a model set when filtering via {nameof(TableEditActionAttribute)}.");
        }
        var tableModel = (ITableModel)multiSwapViewResult.Model;
        multiSwapViewResult
            .WithOobContent(_viewPaths.Table.EditClassToggle, tableModel)
            .WithOobContent(_viewPaths.Table.TableActionList, tableModel);
        foreach (var row in tableModel.Rows)
        {
            multiSwapViewResult.WithOobContent(_viewPaths.Table.Row, (tableModel, row),
                row.TargetDisposition ?? OobTargetDisposition.OuterHtml, row.TargetSelector);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Throws an InvalidOperationException as this filter is designed exclusively for HTMX requests.
    /// Table edit operations require HTMX's out-of-band update capabilities to update individual
    /// rows and components, which cannot be handled through traditional full page renders.
    /// </summary>
    /// <param name="attribute">The TableEditActionAttribute instance.</param>
    /// <param name="cad">The controller action descriptor.</param>
    /// <returns>This method always throws an exception.</returns>
    /// <exception cref="InvalidOperationException">Always thrown as this filter doesn't support non-HTMX requests.</exception>
    protected override Task<string?> GetViewNameForNonHtmxRequest(TableEditActionAttribute attribute, ControllerActionDescriptor cad)
    {
        throw new InvalidOperationException($"{nameof(TableEditActionAttribute)} does not support non-HTMX requests.");
    }
}