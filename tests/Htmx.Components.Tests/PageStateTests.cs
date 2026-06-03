using Htmx.Components.State;
using Microsoft.AspNetCore.DataProtection;

namespace Htmx.Components.Tests;

public class PageStateTests
{
    [Fact]
    public void Encrypted_RoundTripsPartitionedValues()
    {
        var provider = DataProtectionProvider.Create("page-state-tests");
        var source = new PageState(provider);

        source.Set("table", "state", new SavedTableState(3, "Name"));

        var restored = new PageState(provider);
        restored.Load(source.Encrypted);

        var value = restored.Get<SavedTableState>("table", "state");
        Assert.NotNull(value);
        Assert.Equal(3, value.Page);
        Assert.Equal("Name", value.SortColumn);
        Assert.False(restored.IsDirty);
    }

    [Fact]
    public void MutatingState_TracksVersionAndDirtyState()
    {
        var state = new PageState(DataProtectionProvider.Create("page-state-tests"));
        state.Load(null);

        Assert.Equal(0, state.Version);
        Assert.False(state.IsDirty);

        state.Set("filters", "name", "sibyl");
        Assert.Equal(1, state.Version);
        Assert.True(state.IsDirty);

        state.ClearKey("filters", "name");
        Assert.Equal(2, state.Version);
        Assert.Null(state.Get<string>("filters", "name"));
    }

    [Fact]
    public void GetOrCreate_DoesNotRecreateExistingValue()
    {
        var state = new PageState(DataProtectionProvider.Create("page-state-tests"));

        var created = state.GetOrCreate("table", "page", () => 2);
        var existing = state.GetOrCreate("table", "page", () => 9);

        Assert.Equal(2, created);
        Assert.Equal(2, existing);
        Assert.Equal(1, state.Version);
    }

    private sealed record SavedTableState(int Page, string SortColumn);
}
