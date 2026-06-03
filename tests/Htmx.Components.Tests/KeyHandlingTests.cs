using Htmx.Components.Models;
using Htmx.Components.Services;
using Htmx.Components.Table.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Tests;

public class KeyHandlingTests
{
    [Fact]
    public async Task GetKeyPredicate_MatchesSimpleAndCompositeKeys()
    {
        await using var services = TestServices.CreateHtmxServices();
        var registry = new ModelRegistry(
            services,
            services.GetRequiredService<Htmx.Components.Authorization.IResourceOperationRegistry>());

        registry.Register<CompositeWidget, WidgetKey>("widgets", (_, builder) =>
            builder.WithKeySelector(widget => widget.Key));

        var handler = await registry.GetModelHandler<CompositeWidget, WidgetKey>("widgets", ModelUI.Table);
        var predicate = handler.GetKeyPredicate(new WidgetKey(7, "blue")).Compile();

        Assert.True(predicate(new CompositeWidget { Key = new WidgetKey(7, "blue") }));
        Assert.False(predicate(new CompositeWidget { Key = new WidgetKey(8, "blue") }));
    }

    [Fact]
    public void TableRowContext_SerializesKeysAndSanitizesRowIds()
    {
        var row = new TableRowContext<Widget, string>
        {
            Item = new Widget { Id = 1, Name = "Alpha" },
            ModelHandler = null!,
            Key = "alpha beta/1"
        };

        Assert.Equal("\"alpha beta/1\"", row.StringKey);
        Assert.StartsWith("row_", row.RowId);
        Assert.DoesNotContain(" ", row.RowId);
        Assert.DoesNotContain("/", row.RowId);
        Assert.DoesNotContain("\"", row.RowId);
    }

    public sealed class CompositeWidget
    {
        public WidgetKey Key { get; set; } = new(0, "");
    }

    public sealed record WidgetKey(int Id, string Code);
}
