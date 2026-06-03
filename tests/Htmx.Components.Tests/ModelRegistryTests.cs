using Htmx.Components.Authorization;
using Htmx.Components.Models;
using Htmx.Components.Services;
using Microsoft.Extensions.DependencyInjection;
using static Htmx.Components.Authorization.AuthConstants;

namespace Htmx.Components.Tests;

public class ModelRegistryTests
{
    [Fact]
    public async Task Register_BuildsHandlerAndRegistersReadOperation()
    {
        var operationRegistry = new RecordingResourceOperationRegistry();
        await using var services = TestServices.CreateHtmxServices(operationRegistry);
        var registry = new ModelRegistry(services, services.GetRequiredService<IResourceOperationRegistry>());

        registry.Register<Widget, int>("widgets", (_, builder) => builder
            .WithKeySelector(widget => widget.Id)
            .WithQueryable(() => new List<Widget>().AsQueryable()));

        var handler = await registry.GetModelHandler<Widget, int>("widgets", ModelUI.Table);

        Assert.Equal("widgets", handler.TypeId);
        Assert.Equal(typeof(Widget), handler.ModelType);
        Assert.Equal(typeof(int), handler.KeyType);
        Assert.True(handler.CrudFeatures.HasFlag(CrudFeatures.Read));
        Assert.Contains(operationRegistry.Registrations,
            registration => registration == ("widgets", CrudOperations.Read));
    }

    [Fact]
    public async Task GetModelHandler_ThrowsForMissingOrWronglyTypedRegistration()
    {
        await using var services = TestServices.CreateHtmxServices();
        var registry = new ModelRegistry(services, services.GetRequiredService<IResourceOperationRegistry>());
        registry.Register<Widget, int>("widgets", (_, builder) => builder.WithKeySelector(widget => widget.Id));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.GetModelHandler("missing", ModelUI.Table));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.GetModelHandler<Widget, Guid>("widgets", ModelUI.Table));
    }
}
