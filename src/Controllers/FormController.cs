using Htmx.Components.Authorization;
using Htmx.Components.Models;
using Htmx.Components.Services;
using Htmx.Components.Table;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Htmx.Components.Controllers;

/// <summary>
/// Provides CRUD operations for model types through HTMX-enabled endpoints.
/// </summary>
/// <remarks>
/// This controller handles form-based operations including table editing, pagination,
/// sorting, and filtering. It uses dependency injection to resolve model handlers
/// and authorization services for secure operations.
/// </remarks>
[Route("Form")]
public partial class FormController : Controller
{
    private readonly ITableProvider _tableProvider;
    private readonly IModelRegistry _modelRegistry;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuthorizationRequirementFactory _AuthorizationRequirementFactory;
    private readonly ILogger<FormController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormController"/> class.
    /// </summary>
    /// <param name="tableProvider">The table provider for data operations.</param>
    /// <param name="modelRegistry">The model registry for resolving model handlers.</param>
    /// <param name="authorizationService">The authorization service for permission checks.</param>
    /// <param name="AuthorizationRequirementFactory">The factory for creating authorization requirements.</param>
    /// <param name="logger">The logger used for server-side diagnostics.</param>
    public FormController(ITableProvider tableProvider, IModelRegistry modelRegistry,
        IAuthorizationService authorizationService, IAuthorizationRequirementFactory AuthorizationRequirementFactory,
        ILogger<FormController> logger)
    {
        _tableProvider = tableProvider;
        _modelRegistry = modelRegistry;
        _authorizationService = authorizationService;
        _AuthorizationRequirementFactory = AuthorizationRequirementFactory;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the current user is authorized to perform the specified operation on the given model type.
    /// </summary>
    /// <param name="typeId">The model type identifier.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <returns>A task that represents the asynchronous authorization check. The task result is true if authorized; otherwise, false.</returns>
    private async Task<bool> IsAuthorized(string typeId, string operation)
    {
        var requirement = _AuthorizationRequirementFactory.ForOperation(typeId, operation);
        var result = await _authorizationService.AuthorizeAsync(User, null, requirement);
        return result.Succeeded;
    }

    private async Task<(ModelHandler? Handler, IActionResult? Error)> ResolveModelHandler(
        string typeId,
        ModelUI modelUI,
        string? componentId = null)
    {
        try
        {
            var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
            return (modelHandler, null);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Model handler for type id {TypeId} and UI {ModelUI} could not be resolved.", typeId, modelUI);
            return (null, HtmxErrorResult.MissingHandler(componentId));
        }
    }

    private IActionResult ValidationError(string message, string? componentId = null)
        => Request.IsHtmx() ? HtmxErrorResult.Validation(message, componentId) : BadRequest(message);

    private IActionResult CrudError(string message, string? componentId = null)
        => Request.IsHtmx() ? HtmxErrorResult.Crud(message, componentId) : BadRequest(message);

    private IActionResult AuthorizationError(string? componentId = null)
    {
        if (!Request.IsHtmx())
        {
            return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();
        }

        return User.Identity?.IsAuthenticated == true
            ? HtmxErrorResult.Forbidden(componentId)
            : HtmxErrorResult.Unauthorized(componentId);
    }
}
