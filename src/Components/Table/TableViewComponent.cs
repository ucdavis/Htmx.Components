using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Microsoft.AspNetCore.Mvc;

namespace Htmx.Components.Table;

/// <summary>
/// View component that renders table data using the configured table model and view paths.
/// </summary>
/// <remarks>
/// This component integrates with the HTMX Components table system to provide
/// interactive data tables with pagination, sorting, filtering, and CRUD operations.
/// It uses the table model to determine how data should be displayed and which
/// views should be used for rendering different table parts.
/// </remarks>
/// <example>
/// Usage in a Razor view:
/// <code>
/// @await Component.InvokeAsync("Table", tableModel)
/// </code>
/// </example>
public class TableViewComponent : ViewComponent
{
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewComponent"/> class.
    /// </summary>
    /// <param name="viewPaths">The configured view paths for rendering table components.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewPaths"/> is null.</exception>
    public TableViewComponent(ViewPaths viewPaths)
    {
        _viewPaths = viewPaths ?? throw new ArgumentNullException(nameof(viewPaths));
    }

    /// <summary>
    /// Invokes the view component to render a table using the provided table model.
    /// </summary>
    /// <param name="model">The table model containing data, columns, and configuration for rendering.</param>
    /// <returns>A view component result that renders the table using the configured table view.</returns>
    /// <remarks>
    /// This method sets the table view paths on the model and delegates rendering to the
    /// configured table view. The table view is responsible for rendering headers, body,
    /// pagination, and other table elements.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
    public IViewComponentResult Invoke(ITableModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        
        return View(_viewPaths.Table.Table, model);
    }
}