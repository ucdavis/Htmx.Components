using Htmx.Components.Models.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Tests;

public class ActionModelBuilderTests
{
    [Fact]
    public async Task PendingLifecycleHelpers_AddRuntimeDataAttributes()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var action = await new ActionModelBuilder(services)
            .WithLabel("Refresh")
            .WithRequestScope("closest htmx-request-scope")
            .WithRequestIndicator("find [data-hc-request-indicator]")
            .WithPendingDisable("scope")
            .WithPendingDisableSelector("button, input")
            .WithStaleRegion("find [data-hc-stale-region]")
            .BuildAsync();

        Assert.Equal("closest htmx-request-scope", action.Attributes["data-hc-request-scope-selector"]);
        Assert.Equal("find [data-hc-request-indicator]", action.Attributes["data-hc-request-indicator-selector"]);
        Assert.Equal("scope", action.Attributes["data-hc-pending-disable"]);
        Assert.Equal("button, input", action.Attributes["data-hc-pending-disable-selector"]);
        Assert.Equal("find [data-hc-stale-region]", action.Attributes["data-hc-stale-region-selector"]);
    }

    [Fact]
    public void WithPendingDisable_RejectsUnknownModes()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new ActionModelBuilder(services);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithPendingDisable("button"));
    }
}
