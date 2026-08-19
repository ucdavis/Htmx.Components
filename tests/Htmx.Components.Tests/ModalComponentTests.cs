using Htmx.Components.Modal.Models;

namespace Htmx.Components.Tests;

public class ModalComponentTests
{
    [Fact]
    public void ModalComponentHasStableBuiltInName()
    {
        Assert.Equal("Modal", ComponentNames.Modal);
    }

    [Fact]
    public void ModalModelDerivesStableBodyAndTitleIds()
    {
        var model = new ModalModel
        {
            Id = "dashboard-detail-modal",
            Title = "Details",
            Size = ModalSize.Large,
        };

        Assert.Equal("dashboard-detail-modal-body", model.EffectiveBodyTargetId);
        Assert.Equal("dashboard-detail-modal-title", model.TitleId);
        Assert.Equal("max-w-4xl", model.SizeClass);
    }

    [Theory]
    [InlineData(ModalSize.Default, "")]
    [InlineData(ModalSize.Small, "max-w-md")]
    [InlineData(ModalSize.Large, "max-w-4xl")]
    [InlineData(ModalSize.ExtraLarge, "max-w-6xl")]
    [InlineData(ModalSize.Full, "max-w-full w-11/12")]
    public void ModalSizeMapsToDaisyUiWidthClasses(ModalSize size, string expectedClass)
    {
        var model = new ModalModel
        {
            Id = "detail-modal",
            Title = "Details",
            Size = size,
        };

        Assert.Equal(expectedClass, model.SizeClass);
    }

    [Fact]
    public void ModalViewRendersAccessibleStableDaisyUiShell()
    {
        var view = ReadRepoFile("src/Components/Modal/Views/Default.cshtml");

        Assert.Contains("<dialog id=\"@Model.Id\"", view, StringComparison.Ordinal);
        Assert.Contains("class=\"modal\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"@Model.TitleId\"", view, StringComparison.Ordinal);
        Assert.Contains("data-hc-modal", view, StringComparison.Ordinal);
        Assert.Contains("data-hc-modal-box", view, StringComparison.Ordinal);
        Assert.Contains("data-hc-modal-header", view, StringComparison.Ordinal);
        Assert.Contains("data-hc-modal-body-target=\"#@Model.EffectiveBodyTargetId\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"@Model.EffectiveBodyTargetId\" data-hc-modal-body", view, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"@Model.CloseLabel\"", view, StringComparison.Ordinal);
        Assert.Contains("data-hc-modal-close", view, StringComparison.Ordinal);
        Assert.Contains("method=\"dialog\"", view, StringComparison.Ordinal);
        Assert.Contains("modal-backdrop", view, StringComparison.Ordinal);
    }

    [Fact]
    public void ModalRuntimeSupportsOpenAfterSwapCloseResetFocusAndSingleActiveDialog()
    {
        var script = ReadRepoFile("wwwroot/js/htmx-components.js");

        Assert.Contains("\"modal\"", script, StringComparison.Ordinal);
        Assert.Contains("function installModalBehavior()", script, StringComparison.Ordinal);
        Assert.Contains("function resolveHtmxRequestTrigger(detail)", script, StringComparison.Ordinal);
        Assert.Contains("detail?.requestConfig?.elt", script, StringComparison.Ordinal);
        Assert.Contains("data-hc-open-modal", script, StringComparison.Ordinal);
        Assert.Contains("htmx:afterSwap", script, StringComparison.Ordinal);
        Assert.Contains("requestTargetedModalBody(trigger, modal, target)", script, StringComparison.Ordinal);
        Assert.Contains("data-hc-modal-close", script, StringComparison.Ordinal);
        Assert.Contains("body.replaceChildren()", script, StringComparison.Ordinal);
        Assert.Contains("opener.focus()", script, StringComparison.Ordinal);
        Assert.Contains("if (activeModal && activeModal !== modal)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void TailwindExtractedClassesIncludeModalShellClasses()
    {
        var classes = ReadRepoFile("tools/tailwind/extracted-css-classes.css");

        foreach (var className in new[]
        {
            "modal",
            "modal-box",
            "modal-backdrop",
            "btn-circle",
            "btn-ghost",
            "max-w-md",
            "max-w-4xl",
            "max-w-6xl",
            "max-w-full",
            "w-11/12"
        })
        {
            Assert.Contains(className, classes, StringComparison.Ordinal);
        }
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
