using Htmx.Components.Authorization;
using Htmx.Components.Models;
using Htmx.Components.Models.Builders;
using Htmx.Components.Services;
using Htmx.Components.Table.Models;
using Microsoft.Extensions.DependencyInjection;
using static Htmx.Components.Authorization.AuthConstants;

namespace Htmx.Components.Tests;

public class TableDataSourceTests
{
    [Fact]
    public async Task BuildTableModelAndFetchPageAsync_UsesSyncExecutionForNonEfQueryable()
    {
        var rows = new List<Widget>
        {
            new() { Id = 3, Name = "Charlie" },
            new() { Id = 1, Name = "Alpha" },
            new() { Id = 2, Name = "Beta" },
        };
        var handler = await BuildHandler(builder => builder
            .WithQueryable(() => rows.AsQueryable())
            .WithTable(table => table.AddSelectorColumn(widget => widget.Name)));

        var tableModel = await handler.BuildTableModelAndFetchPageAsync(new TableState { PageSize = 2 });

        Assert.Equal(2, tableModel.PageCount);
        Assert.Equal(["Alpha", "Beta"], tableModel.Rows.Select(row => row.Item.Name));
        Assert.All(tableModel.Rows, row => Assert.Equal(row.Item.Id, row.Key));
    }

    [Fact]
    public async Task WithRows_BuildsPagedTableFromEnumerableRows()
    {
        var rows = new List<Widget>
        {
            new() { Id = 1, Name = "Alpha" },
            new() { Id = 2, Name = "Beta" },
            new() { Id = 3, Name = "Charlie" },
        };
        var handler = await BuildHandler(builder => builder
            .WithRows(() => rows)
            .WithTable(table => table.AddSelectorColumn(widget => widget.Name)));

        var tableModel = await handler.BuildTableModelAndFetchPageAsync(new TableState { Page = 2, PageSize = 2 });

        Assert.Equal(2, tableModel.PageCount);
        Assert.Equal(["Charlie"], tableModel.Rows.Select(row => row.Item.Name));
    }

    [Fact]
    public async Task WithRowsAsync_BuildsPagedTableFromAsyncRows()
    {
        var rows = new List<Widget>
        {
            new() { Id = 1, Name = "Alpha" },
            new() { Id = 2, Name = "Beta" },
            new() { Id = 3, Name = "Charlie" },
        };
        var handler = await BuildHandler(builder => builder
            .WithRowsAsync(() => Task.FromResult<IReadOnlyList<Widget>>(rows))
            .WithTable(table => table.AddSelectorColumn(widget => widget.Name)));

        var tableModel = await handler.BuildTableModelAndFetchPageAsync(new TableState { SortColumn = "Name", SortDirection = "desc", PageSize = 2 });

        Assert.Equal(2, tableModel.PageCount);
        Assert.Equal(["Charlie", "Beta"], tableModel.Rows.Select(row => row.Item.Name));
    }

    [Fact]
    public async Task NonEfReadSourceSupportsEditAndCancelLookups()
    {
        var rows = new List<Widget>
        {
            new() { Id = 1, Name = "Alpha" },
            new() { Id = 2, Name = "Beta" },
        };
        var handler = await BuildHandler(builder => builder
            .WithRows(() => rows)
            .WithTable(table => table.AddSelectorColumn(widget => widget.Name)));

        var query = await handler.GetReadQueryAsync();
        var editItem = await handler.SingleOrDefaultAsync(query.Where(handler.GetKeyPredicate(2)));
        var cancelItem = await handler.SingleAsync(query.Where(handler.GetKeyPredicate(1)));

        Assert.Equal("Beta", editItem?.Name);
        Assert.Equal("Alpha", cancelItem.Name);
    }

    [Fact]
    public async Task WithRowsCanBeCombinedWithCreateAndUpdateOperations()
    {
        var rows = new List<Widget>
        {
            new() { Id = 1, Name = "Alpha" },
        };
        var handler = await BuildHandler(builder => builder
            .WithRows(() => rows)
            .WithCreate(widget =>
            {
                widget.Id = rows.Max(row => row.Id) + 1;
                rows.Add(widget);
                return Task.FromResult(new Result<Widget>(widget));
            })
            .WithUpdate(widget =>
            {
                var existing = rows.Single(row => row.Id == widget.Id);
                existing.Name = widget.Name;
                return Task.FromResult(new Result<Widget>(existing));
            })
            .WithTable(table => table.AddSelectorColumn(widget => widget.Name)));

        var created = await handler.CreateModel!(new Widget { Name = "Beta" });
        var updated = await handler.UpdateModel!(new Widget { Id = 2, Name = "Bravo" });
        var tableModel = await handler.BuildTableModelAndFetchPageAsync();

        Assert.False(created.IsError);
        Assert.False(updated.IsError);
        Assert.True(handler.CrudFeatures.HasFlag(CrudFeatures.Read));
        Assert.True(handler.CrudFeatures.HasFlag(CrudFeatures.Create));
        Assert.True(handler.CrudFeatures.HasFlag(CrudFeatures.Update));
        Assert.Equal(["Alpha", "Bravo"], tableModel.Rows.Select(row => row.Item.Name));
    }

    private static async Task<ModelHandler<Widget, int>> BuildHandler(Action<ModelHandlerBuilder<Widget, int>> configure)
    {
        var operationRegistry = new RecordingResourceOperationRegistry();
        var services = TestServices.CreateHtmxServices(operationRegistry);
        var registry = new ModelRegistry(services, services.GetRequiredService<IResourceOperationRegistry>());

        registry.Register<Widget, int>("widgets", (_, builder) =>
        {
            builder.WithKeySelector(widget => widget.Id);
            configure(builder);
        });

        var handler = await registry.GetModelHandler<Widget, int>("widgets", ModelUI.Table);
        Assert.Contains(operationRegistry.Registrations,
            registration => registration == ("widgets", CrudOperations.Read));
        return handler;
    }
}
