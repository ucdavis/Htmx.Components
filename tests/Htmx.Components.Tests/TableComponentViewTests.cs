namespace Htmx.Components.Tests;

public class TableComponentViewTests
{
    [Fact]
    public void TableViewMarksStickyHeaderAndFooterForContextualOffsets()
    {
        var view = ReadRepoFile("src/Components/Table/Views/_Table.cshtml");

        Assert.Contains("data-hc-table-sticky-header", view, StringComparison.Ordinal);
        Assert.Contains("data-hc-table-sticky-footer", view, StringComparison.Ordinal);
    }

    [Fact]
    public void TableOverridesResetStickyOffsetsInsideModals()
    {
        var css = ReadRepoFile("wwwroot/css/table-overrides.css");

        Assert.Contains("[data-hc-modal] [data-hc-modal-box]", css, StringComparison.Ordinal);
        Assert.Contains("overflow: hidden;", css, StringComparison.Ordinal);
        Assert.Contains("[data-hc-modal] [data-hc-modal-body]", css, StringComparison.Ordinal);
        Assert.Contains("overflow: auto;", css, StringComparison.Ordinal);
        Assert.Contains("[data-hc-modal] [data-hc-table-sticky-header]", css, StringComparison.Ordinal);
        Assert.Contains("[data-hc-modal] [data-hc-table-sticky-header] th", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--color-base-100, Canvas);", css, StringComparison.Ordinal);
        Assert.Contains("top: 0;", css, StringComparison.Ordinal);
        Assert.Contains("[data-hc-modal] [data-hc-table-sticky-footer]", css, StringComparison.Ordinal);
        Assert.Contains("bottom: 0;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void EditableInputsUseNoSwapFieldUpdates()
    {
        var view = ReadRepoFile("src/Views/Shared/_Input.cshtml");

        Assert.DoesNotContain("hx-post=\"@Url.Action(\"SetValue\", \"Form\", routeValues)\" hx-trigger=\"blur\" hx-vals", view, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-post=\"@Url.Action(\"SetValue\", \"Form\", routeValues)\" hx-trigger=\"change\" hx-vals", view, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"blur\" hx-swap=\"none\"", view, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"change\" hx-swap=\"none\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void TableActionButtonsDoNotSubmitSurroundingForms()
    {
        var cellActions = ReadRepoFile("src/Components/Table/Views/_TableCellActionList.cshtml");
        var tableActions = ReadRepoFile("src/Components/Table/Views/_TableActionList.cshtml");

        Assert.Contains("type=\"button\"", cellActions, StringComparison.Ordinal);
        Assert.Contains("type=\"button\"", tableActions, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(string path)
        => File.ReadAllText(Path.Combine(FindRepoRoot(), path));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Htmx.Components.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the Htmx.Components repository root.");
    }
}
