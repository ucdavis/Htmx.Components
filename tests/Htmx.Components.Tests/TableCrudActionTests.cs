using Htmx.Components.Authorization;
using Htmx.Components.Models;
using Htmx.Components.Models.Builders;
using Htmx.Components.Services;
using Htmx.Components.Table.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Tests;

public class TableCrudActionTests
{
    [Fact]
    public async Task BuiltInCrudActionsUseOobOnlySwaps()
    {
        var rows = new List<Widget>
        {
            new() { Id = 1, Name = "Alpha" }
        };
        var handler = await BuildHandler(builder => builder
            .WithRows(() => rows)
            .WithCreate(widget => Task.FromResult(new Result<Widget>(widget)))
            .WithUpdate(widget => Task.FromResult(new Result<Widget>(widget)))
            .WithDelete(_ => Task.FromResult(Result.Ok()))
            .WithTable(table => table
                .WithCrudActions()
                .AddSelectorColumn(widget => widget.Name)
                .AddCrudDisplayColumn()));

        var table = await handler.BuildTableModelAndFetchPageAsync();
        var tableActions = await table.GetActionsAsync();
        var rowActions = await table.Columns.Single(column => column.Header == "Actions")
            .GetActionsAsync(table.Rows.Single());

        var actions = tableActions.Concat(rowActions).ToList();

        Assert.Contains(actions, action => action.Label == "Add New");
        Assert.Contains(actions, action => action.Label == "Edit");
        Assert.Contains(actions, action => action.Label == "Delete");
        Assert.All(actions, action => Assert.Equal("none", action.Attributes["hx-swap"]));
    }

    [Fact]
    public async Task BuiltInEditingCrudActionsUseOobOnlySwaps()
    {
        var handler = await BuildHandler(builder => builder
            .WithRows(() => [new Widget { Id = 1, Name = "Alpha" }])
            .WithUpdate(widget => Task.FromResult(new Result<Widget>(widget)))
            .WithTable(table => table.AddCrudDisplayColumn()));
        var table = await handler.BuildTableModelAsync();
        var row = new TableRowContext<Widget, int>
        {
            Item = new Widget { Id = 1, Name = "Alpha" },
            ModelHandler = handler,
            Key = 1,
            IsEditing = true
        };

        var actions = await table.Columns.Single(column => column.Header == "Actions")
            .GetActionsAsync(row);

        Assert.Contains(actions, action => action.Label == "Save");
        Assert.Contains(actions, action => action.Label == "Cancel");
        Assert.All(actions, action => Assert.Equal("none", action.Attributes["hx-swap"]));
    }

    private static async Task<ModelHandler<Widget, int>> BuildHandler(Action<ModelHandlerBuilder<Widget, int>> configure)
    {
        var services = TestServices.CreateHtmxServices();
        var registry = new ModelRegistry(
            services,
            services.GetRequiredService<IResourceOperationRegistry>());

        registry.Register<Widget, int>("widgets", (_, builder) =>
        {
            builder.WithKeySelector(widget => widget.Id);
            configure(builder);
        });

        return await registry.GetModelHandler<Widget, int>("widgets", ModelUI.Table);
    }
}
