using System.Linq.Expressions;
using FastExpressionCompiler;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Htmx.Components.Models.Builders;


/// <summary>
/// Abstracts the process of creating a <see cref="TableModel{T, TKey}"/>
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The key type</typeparam>
public class TableModelBuilder<T, TKey> : BuilderBase<TableModelBuilder<T, TKey>, TableModel<T, TKey>>
    where T : class
{
    private readonly TableModelConfig<T, TKey> _config = new();

    internal TableModelBuilder(Expression<Func<T, TKey>> keySelector, ModelHandler<T, TKey> modelHandler, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _config.KeySelector = keySelector;
        _config.ModelHandler = modelHandler;
        _config.TypeId = modelHandler.TypeId;
    }


    /// <summary>
    /// Adds a TableColumnModel configured to be used as a value selector
    /// </summary>
    /// <param name="selector">The property selector expression</param>
    /// <param name="configure">Optional configuration action for the column</param>
    /// <returns>The table model builder for method chaining</returns>
    public TableModelBuilder<T, TKey> AddSelectorColumn(Expression<Func<T, object>> selector,
        Action<TableColumnModelBuilder<T, TKey>>? configure = null)
    {
        AddBuildTask(async () =>
        {
            var propertyName = selector.GetPropertyName();
            var header = propertyName.Humanize(LetterCasing.Title);
            var config = new TableColumnModelConfig<T, TKey>
            {
                Display = new TableColumnDisplayOptions
                {
                    Header = header,
                    DataName = propertyName,
                    ColumnType = ColumnType.ValueSelector
                },
                DataOptions = new TableColumnDataOptions<T, TKey>
                {
                    SelectorExpression = selector,
                    ModelHandler = _config.ModelHandler!,
                },
                Behavior = new TableColumnBehaviorOptions
                {
                    Sortable = true,
                    Filterable = true,
                    IsEditable = false
                },
                FilterOptions = new()
            };
            var builder = new TableColumnModelBuilder<T, TKey>(config, ServiceProvider);
            configure?.Invoke(builder);
            var columnModel = await builder.BuildAsync();
            _config.Columns.Add(columnModel);
        });
        return this;
    }

    /// <summary>
    /// Adds a TableColumnModel configured to be used as a display column
    /// </summary>
    /// <param name="header"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public TableModelBuilder<T, TKey> AddDisplayColumn(string header, Action<TableColumnModelBuilder<T, TKey>>? configure = null)
    {
        AddBuildTask(async () =>
        {
            var config = new TableColumnModelConfig<T, TKey>
            {
                Display = new TableColumnDisplayOptions
                {
                    Header = header,
                    ColumnType = ColumnType.Display
                },
                Behavior = new TableColumnBehaviorOptions
                {
                    Sortable = false,
                    Filterable = false
                },
                DataOptions = new TableColumnDataOptions<T, TKey>
                {
                    ModelHandler = _config.ModelHandler!
                }
            };
            var builder = new TableColumnModelBuilder<T, TKey>(config, ServiceProvider);
            configure?.Invoke(builder);
            var columnModel = await builder.BuildAsync();
            _config.Columns.Add(columnModel);
        });
        return this;
    }

    /// <summary>
    /// Adds a display-only column with standard CRUD actions (Edit, Delete) to the table.
    /// This is a convenience method that creates an action column with the default CRUD operations.
    /// </summary>
    /// <param name="header">The header text for the actions column (defaults to "Actions")</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableModelBuilder<T, TKey> AddCrudDisplayColumn(string header = "Actions")
    {
        return AddDisplayColumn("Actions", col => col.WithCrudActions());
    }


    /// <summary>
    /// Adds table-level actions that appear above the table (such as "Add New" buttons).
    /// These actions operate on the entire table rather than individual rows.
    /// </summary>
    /// <param name="actionsFactory">A function that configures table-level actions using an action set builder</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableModelBuilder<T, TKey> WithActions(Action<TableModel<T, TKey>, ActionSetBuilder> actionsFactory)
    {
        AddBuildTask(() =>
        {
            _config.ActionsFactories.Add(async (tableModel) =>
            {
                var actionSetBuilder = new ActionSetBuilder(ServiceProvider);
                actionsFactory.Invoke(tableModel, actionSetBuilder);
                var actionSet = await actionSetBuilder.BuildAsync();
                return actionSet.Items.Cast<ActionModel>();
            });
        });
        return this;
    }

    /// <summary>
    /// Adds standard CRUD table-level actions to the table.
    /// Currently adds an "Add New" action if the model handler supports Create operations.
    /// The actions are based on the CRUD features enabled in the model handler.
    /// </summary>
    /// <returns>The current builder instance for method chaining</returns>
    public TableModelBuilder<T, TKey> WithCrudActions()
    {
        var typeId = _config.TypeId!;
        var canCreate = (_config.ModelHandler?.CrudFeatures ?? CrudFeatures.None).HasFlag(CrudFeatures.Create);
        if (!canCreate)
            return this;
        return WithActions((table, actions) =>
            actions.AddAction(action => action
                .WithLabel("Add New")
                .WithIcon("fas fa-plus mr-1")
                .WithHxPost($"/Form/{typeId}/Table/Create")
        ));
    }

    /// <summary>
    /// Sets the unique type identifier for this table model.
    /// This identifier is used for routing, authorization, and UI component registration.
    /// </summary>
    /// <param name="typeId">A unique string identifier for the model type</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TableModelBuilder<T, TKey> WithTypeId(string typeId)
    {
        _config.TypeId = typeId;
        return this;
    }

    /// <summary>
    /// Builds the configured table model and performs final setup operations.
    /// This method sets up column relationships, configures default filtering for filterable columns,
    /// and enables editing for columns that have input models and appropriate CRUD permissions.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation. The task result contains the configured table model.</returns>
    protected override Task<TableModel<T, TKey>> BuildImpl()
    {
        var model = new TableModel<T, TKey>(_config);
        foreach (var column in model.Columns)
        {
            column.Table = model;
        }
        // Set up default filtering for columns that are filterable but do not have a custom filter defined
        foreach (var column in model.Columns.Where(c => c.Filterable && c.Filter == null))
        {
            column.Filter = (query, value) =>
            {
                return TableColumnHelper.Filter(query, value, column);
            };
        }
        // Set IsEditable for columns that have GetInputModel defined
        if ((model.ModelHandler?.CrudFeatures.HasFlag(CrudFeatures.Create) ?? false)
            || (model.ModelHandler?.CrudFeatures.HasFlag(CrudFeatures.Update) ?? false))
        {
            foreach (var column in model.Columns.Where(c => c.GetInputModel != null))
            {
                column.IsEditable = true;
            }
        }
        return Task.FromResult(model);
    }
}
