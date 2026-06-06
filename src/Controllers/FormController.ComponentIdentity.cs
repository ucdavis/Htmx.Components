using Htmx.Components.Table;
using Microsoft.AspNetCore.Mvc;

namespace Htmx.Components.Controllers;

public partial class FormController
{
    private IActionResult? ValidateTableComponentId(string? componentId, out string normalizedComponentId)
    {
        normalizedComponentId = "";
        if (string.IsNullOrWhiteSpace(componentId))
        {
            return ValidationError("This table action could not be matched to a component.");
        }

        normalizedComponentId = TableComponentIdentity.Ensure(componentId);
        return null;
    }
}
