using Htmx.Components.Authorization;
using Htmx.Components.Services;
using Htmx.Components.Table;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="FormController"/> class.
    /// </summary>
    /// <param name="tableProvider">The table provider for data operations.</param>
    /// <param name="modelRegistry">The model registry for resolving model handlers.</param>
    /// <param name="authorizationService">The authorization service for permission checks.</param>
    /// <param name="AuthorizationRequirementFactory">The factory for creating authorization requirements.</param>
    public FormController(ITableProvider tableProvider, IModelRegistry modelRegistry,
        IAuthorizationService authorizationService, IAuthorizationRequirementFactory AuthorizationRequirementFactory)
    {
        _tableProvider = tableProvider;
        _modelRegistry = modelRegistry;
        _authorizationService = authorizationService;
        _AuthorizationRequirementFactory = AuthorizationRequirementFactory;
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
}