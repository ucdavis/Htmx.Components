using System.Linq.Expressions;
using FastExpressionCompiler;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Models.Builders;


/// <summary>
/// Provides a fluent API for configuring table columns, including display options, filtering, editing capabilities, and actions.
/// This builder allows detailed customization of how data is presented and interacted with in table views.
/// </summary>
/// <typeparam name="T">The model type being displayed in the table</typeparam>
/// <typeparam name="TKey">The type of the model's primary key</typeparam>
public class TableColumnModelBuilder<T, TKey> : BuilderBase<TableColumnModelBuilder<T, TKey>, TableColumnModel<T, TKey>>
    where T : class
{
    private readonly TableColumnModelConfig<T, TKey> _config;
    private readonly ViewPaths _viewPaths;

    internal TableColumnModelBuilder(TableColumnModelConfig<T, TKey> config, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _config = config;
        _viewPaths = serviceProvider.GetRequiredService<ViewPaths>();
    }

    /// <summary>
    /// Sets the header text displayed for this column in the table.
    /// This is the text that appears in the column header row.
    /// </summary>
    /// <param name="header">The text to display in the column header</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableColumnModelBuilder<T, TKey> WithHeader(string header)
    {
        _config.Display.Header = header;
        return this;
    }

    /// <summary>
    /// Configures whether this column supports inline editing.
    /// When enabled, clicking on cells in this column will display input controls for editing.
    /// Requires that an input model builder is registered for this column's property.
    /// </summary>
    /// <param name="isEditable">True to enable inline editing, false to disable it</param>
    /// <returns>The current builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no input model builder is found for this column's property
    /// </exception>
    public TableColumnModelBuilder<T, TKey> WithEditable(bool isEditable = true)
    {
        if (!(_config.DataOptions.ModelHandler?.InputModelBuilders?.TryGetValue(_config.Display.DataName, out var inputModelBuilder) == true))
        {
            throw new InvalidOperationException($"No input model builder found for column '{_config.Display.DataName}'. Ensure that the input model is registered in the ModelHandler.");
        }
        _config.Behavior.IsEditable = isEditable;
        if (isEditable)
        {
            _config.InputOptions.GetInputModel = async (rowContext) =>
            {
                var inputModel = await inputModelBuilder.Invoke(rowContext.ModelHandler);
                inputModel.ObjectValue = _config.DataOptions.SelectorFunc!(rowContext.Item);
                return inputModel;
            };
        }
        return this;
    }

    /// <summary>
    /// Specifies a custom partial view to use for rendering the cells in this column.
    /// This allows complete customization of how the cell content is displayed.
    /// </summary>
    /// <param name="cellPartial">The path or name of the partial view to use for cell rendering</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableColumnModelBuilder<T, TKey> WithCellPartial(string cellPartial)
    {
        _config.Display.CellPartialView = cellPartial;
        return this;
    }

    /// <summary>
    /// Specifies a custom partial view to use for rendering the filter controls for this column.
    /// This allows customization of how filtering UI is presented and also enables editing for the column.
    /// </summary>
    /// <param name="filterPartial">The path or name of the partial view to use for filter rendering</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableColumnModelBuilder<T, TKey> WithFilterPartial(string filterPartial)
    {
        _config.Display.FilterPartialView = filterPartial;
        _config.Behavior.IsEditable = true;
        return this;
    }

    /// <summary>
    /// Configures a filter function that can be applied to the queryable data source for this column.
    /// This enables filtering functionality based on user input for this column.
    /// </summary>
    /// <param name="filter">A function that takes the queryable data source and a filter value, returning a filtered queryable</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableColumnModelBuilder<T, TKey> WithFilter(Func<IQueryable<T>, string, IQueryable<T>> filter)
    {
        _config.FilterOptions.Filter = filter;
        _config.Behavior.Filterable = true;
        return this;
    }

    /// <summary>
    /// Configures a range filter function for columns that support range-based filtering (such as dates or numbers).
    /// This allows users to filter data between two values. Currently experimental and may not work as expected.
    /// </summary>
    /// <param name="rangeFilter">A function that takes the queryable data source and two filter values (start and end), returning a filtered queryable</param>
    /// <returns>The current builder instance for method chaining</returns>
    /// <remarks>
    /// This feature is not fully tested and may require additional work to support different column types properly.
    /// </remarks>
    public TableColumnModelBuilder<T, TKey> WithRangeFilter(Func<IQueryable<T>, string, string, IQueryable<T>> rangeFilter)
    {
        //TODO: not tested and probably won't work. need to figure out how to support different column types
        _config.FilterOptions.RangeFilter = rangeFilter;
        _config.Behavior.Filterable = true;
        if (string.IsNullOrWhiteSpace(_config.Display.FilterPartialView))
        {
            _config.Display.FilterPartialView = _viewPaths.Table.FilterDateRange;
        }
        return this;
    }

    /// <summary>
    /// Adds custom actions to this column that will be displayed for each row.
    /// Actions are interactive elements like buttons or links that users can click to perform operations.
    /// If no custom cell partial view is specified, will use the default action list partial view.
    /// </summary>
    /// <param name="actionsFactory">A function that configures actions for each row context using an action set builder</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableColumnModelBuilder<T, TKey> WithActions(Action<TableRowContext<T, TKey>, ActionSetBuilder> actionsFactory)
    {
        _config.ActionOptions.ActionsFactories.Add(async (rowContext) =>
        {
            var actionSetBuilder = new ActionSetBuilder(ServiceProvider);
            actionsFactory.Invoke(rowContext, actionSetBuilder);
            var actionSet = await actionSetBuilder.BuildAsync();
            return actionSet.Items.Cast<ActionModel>();
        });
        if (string.IsNullOrWhiteSpace(_config.Display.CellPartialView))
        {
            _config.Display.CellPartialView = _viewPaths.Table.CellActionList;
        }
        return this;
    }

    /// <summary>
    /// Adds standard CRUD (Create, Read, Update, Delete) actions to this column.
    /// The available actions depend on the CRUD features enabled in the model handler.
    /// In edit mode, shows Save and Cancel actions. In view mode, shows Edit and Delete actions based on permissions.
    /// </summary>
    /// <returns>The current builder instance for method chaining</returns>
    public TableColumnModelBuilder<T, TKey> WithCrudActions()
    {
        WithActions((row, actions) =>
        {
            var typeId = row.ModelHandler.TypeId;
            if (row.IsEditing)
            {
                actions.AddAction(action => action
                    .WithLabel("Save")
                    .WithIcon("fas fa-save")
                    .WithHxPost($"/Form/{typeId}/Table/Save"));
                actions.AddAction(action => action
                    .WithLabel("Cancel")
                    .WithIcon("fas fa-times")
                    .WithHxPost($"/Form/{typeId}/Table/CancelEdit"));
            }
            else
            {
                var crudFeatures = _config.DataOptions.ModelHandler?.CrudFeatures ?? CrudFeatures.None;
                if (crudFeatures.HasFlag(CrudFeatures.Update))
                {
                    actions.AddAction(action => action
                        .WithLabel("Edit")
                        .WithIcon("fas fa-edit")
                        .WithHxPost($"/Form/{typeId}/Table/Edit?key={row.Key}"));
                }
                if (crudFeatures.HasFlag(CrudFeatures.Delete))
                {
                    actions.AddAction(action => action
                        .WithLabel("Delete")
                        .WithIcon("fas fa-trash")
                        .WithClass("text-red-600")
                        .WithHxPost($"/Form/{typeId}/Table/Delete?key={row.Key}"));
                }
            }
        });
        return this;
    }

    /// <summary>
    /// Builds the configured table column model.
    /// This method is called internally by the builder framework to create the final column model.
    /// Compiles any selector expressions for performance if needed.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation. The task result contains the configured table column model.</returns>
    protected override Task<TableColumnModel<T, TKey>> BuildImpl()
    {
        if (_config.DataOptions.SelectorFunc == null && _config.DataOptions.SelectorExpression != null)
        {
            _config.DataOptions.SelectorFunc = _config.DataOptions.SelectorExpression.CompileFast();
        }

        var model = new TableColumnModel<T, TKey>(_config);
        return Task.FromResult(model);
    }
}


