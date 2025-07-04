using System.Linq.Expressions;
using Htmx.Components.Authorization;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.State;
using Htmx.Components.Table;
using Microsoft.Extensions.DependencyInjection;
using static Htmx.Components.Authorization.AuthConstants;

namespace Htmx.Components.Models.Builders;

public class ModelHandlerBuilder<T, TKey> : BuilderBase<ModelHandlerBuilder<T, TKey>, ModelHandler<T, TKey>>
    where T : class, new()
{
    private readonly IResourceOperationRegistry _resourceOperationRegistry;
    private readonly ModelHandlerOptions<T, TKey> _options = new();
    private readonly ITableProvider _tableProvider;
    private readonly IPageState _pageState;

    internal ModelHandlerBuilder(IServiceProvider serviceProvider, string typeId, IResourceOperationRegistry resourceOperationRegistry)
        : base(serviceProvider)
    {
        _resourceOperationRegistry = resourceOperationRegistry;
        _options.TypeId = typeId;
        _options.ModelUI = ModelUI.Table;
        _options.ServiceProvider = serviceProvider;
        _tableProvider = serviceProvider.GetRequiredService<ITableProvider>();
        _pageState = serviceProvider.GetRequiredService<IPageState>();
    }

    public ModelHandlerBuilder<T, TKey> WithTypeId(string typeId)
    {
        _options.TypeId = typeId;
        return this;
    }

    public ModelHandlerBuilder<T, TKey> WithKeySelector(Expression<Func<T, TKey>> keySelector)
    {
        _options.KeySelector = keySelector;
        return this;
    }

    public ModelHandlerBuilder<T, TKey> WithQueryable(Func<IQueryable<T>> getQueryable)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Read;
        _options.Crud.GetQueryable = getQueryable;
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Read));
        return this;
    }

    public ModelHandlerBuilder<T, TKey> WithCreate(Func<T, Task<Result<T>>> createModel)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Create;
        _options.Crud.CreateModel = createModel;
        _options.Crud.GetCreateActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Create",
            Icon = "fas fa-plus mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-post", $"/Form/{_options.TypeId}/{_options.ModelUI}/Create" },
            }
        });
        SetCancelActionModel();
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Create));
        return this;
    }

    public ModelHandlerBuilder<T, TKey> WithUpdate(Func<T, Task<Result<T>>> updateModel)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Update;
        _options.Crud.UpdateModel = updateModel;
        _options.Crud.GetUpdateActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Update",
            Icon = "fas fa-edit mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-post", $"/Form/{_options.TypeId}/{_options.ModelUI}/Update" },
            }
        });
        SetCancelActionModel();
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Update));
        return this;
    }

    private void SetCancelActionModel()
    {
        if (_options.Crud.GetCancelActionModel != null)
            return;
        _options.Crud.GetCancelActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Cancel",
            Icon = "fas fa-times mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-get", $"/Form/{_options.TypeId}/{_options.ModelUI}/Cancel" },
            }
        });
    }

    public ModelHandlerBuilder<T, TKey> WithDelete(Func<TKey, Task<Result>> deleteModel)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Delete;
        _options.Crud.DeleteModel = deleteModel;
        _options.Crud.GetDeleteActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Delete",
            Icon = "fas fa-trash mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-delete", $"/Form/{_options.TypeId}/{_options.ModelUI}/Delete" },
            }
        });
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Delete));
        return this;
    }

    public ModelHandlerBuilder<T, TKey> WithTable(Action<TableModelBuilder<T, TKey>> configure)
    {
        _options.Table.ConfigureTableModel = configure;
        return this;
    }

    public ModelHandlerBuilder<T, TKey> WithInput<TProp>(Expression<Func<T, TProp>> propertySelector,
        Action<InputModelBuilder<T, TProp>> configure)
    {
        _options.Inputs.InputModelBuilders.TryAdd(propertySelector.GetPropertyName(), async (modelHandler) =>
        {
            var builder = new InputModelBuilder<T, TProp>(ServiceProvider, propertySelector);
            configure(builder);
            var inputModel = await builder.BuildAsync();
            inputModel.ModelHandler = modelHandler;
            return inputModel;
        });
        return this;
    }

    protected override Task<ModelHandler<T, TKey>> BuildImpl()
    {
        var handler = new ModelHandler<T, TKey>(_options, _tableProvider, _pageState);
        return Task.FromResult(handler);
    }

}
