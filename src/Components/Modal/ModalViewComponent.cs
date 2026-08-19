using Htmx.Components.Models;
using Htmx.Components.Modal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Htmx.Components.Modal;

/// <summary>
/// View component that renders a reusable DaisyUI dialog shell for HTMX-loaded content.
/// </summary>
public class ModalViewComponent : ViewComponent
{
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalViewComponent"/> class.
    /// </summary>
    /// <param name="viewPaths">The configured view paths for rendering components.</param>
    public ModalViewComponent(ViewPaths viewPaths)
    {
        _viewPaths = viewPaths ?? throw new ArgumentNullException(nameof(viewPaths));
    }

    /// <summary>
    /// Invokes the view component to render a modal shell.
    /// </summary>
    /// <param name="model">The modal model.</param>
    /// <returns>A view component result that renders the modal shell.</returns>
    public IViewComponentResult Invoke(ModalModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            throw new ArgumentException("A stable modal id is required.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            throw new ArgumentException("A modal title is required.", nameof(model));
        }

        return View(_viewPaths.Modal, model);
    }
}
