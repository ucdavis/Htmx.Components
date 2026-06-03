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
            return BadRequest("A table component id is required.");
        }

        normalizedComponentId = TableComponentIdentity.Ensure(componentId);
        return null;
    }
}
