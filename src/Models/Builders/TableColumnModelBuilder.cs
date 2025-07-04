using System.Linq.Expressions;
using FastExpressionCompiler;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Models.Builders;


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

    public TableColumnModelBuilder<T, TKey> WithHeader(string header)
    {
        _config.Display.Header = header;
        return this;
    }

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

    public TableColumnModelBuilder<T, TKey> WithCellPartial(string cellPartial)
    {
        _config.Display.CellPartialView = cellPartial;
        return this;
    }

    public TableColumnModelBuilder<T, TKey> WithFilterPartial(string filterPartial)
    {
        _config.Display.FilterPartialView = filterPartial;
        _config.Behavior.IsEditable = true;
        return this;
    }

    public TableColumnModelBuilder<T, TKey> WithFilter(Func<IQueryable<T>, string, IQueryable<T>> filter)
    {
        _config.FilterOptions.Filter = filter;
        _config.Behavior.Filterable = true;
        return this;
    }

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


