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
/// Abstracts the process of creating a <see cref="TableModel<typeparamref name="T"/>"/>
/// </summary>
/// <typeparam name="T"></typeparam>
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
    /// <param name="header"></param>
    /// <param name="selector"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
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

    public TableModelBuilder<T, TKey> AddCrudDisplayColumn(string header = "Actions")
    {
        return AddDisplayColumn("Actions", col => col.WithCrudActions());
    }


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

    public TableModelBuilder<T, TKey> WithTypeId(string typeId)
    {
        _config.TypeId = typeId;
        return this;
    }

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
