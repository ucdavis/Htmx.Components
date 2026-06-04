using System.Text.Json;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Table;
using Htmx.Components.Utilities;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Htmx.Components.Authorization.AuthConstants;
using static Htmx.Components.State.PageStateConstants;
using Htmx.Components.Table.Internal;

namespace Htmx.Components.Controllers;

public partial class FormController
{
    /// <summary>
    /// Initiates editing mode for a specific record, loading the item into an editable form.
    /// This action prepares the UI for editing an existing record identified by the provided key.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being edited</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="key">The unique key identifying the record to edit</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result containing the edit form UI</returns>
    [HttpPost("{typeId}/{modelUI}/Edit")]
    [TableEditAction]
    public async Task<IActionResult> Edit(string typeId, ModelUI modelUI, string key, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(EditImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            key, scopedComponentId, modelHandler);
        return result!;
    }

    private async Task<IActionResult> EditImpl<T, TKey>(string stringKey, string componentId, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
            return AuthorizationError(componentId);
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Update))
            return AuthorizationError(componentId);

        var key = (TKey)JsonSerializer.Deserialize(stringKey, modelHandler.KeyType)!;
        var editingItem = await modelHandler.GetQueryable!()
            .Where(modelHandler.GetKeyPredicate(key))
            .SingleOrDefaultAsync();
        if (editingItem == null)
            return CrudError("The selected item could not be found.", componentId);
        var pageState = this.GetPageState();
        var formStatePartition = TableComponentIdentity.FormStatePartition(componentId);
        pageState.Set(formStatePartition, FormStateKeys.EditingItem, editingItem);
        pageState.Set(formStatePartition, FormStateKeys.EditingExistingRecord, true);

        var tableModel = await modelHandler.BuildTableModelAsync();
        tableModel.ComponentId = componentId;
        tableModel.Rows.Add(new TableRowContext<T, TKey>
        {
            Item = editingItem,
            ModelHandler = modelHandler,
            Key = key,
            TargetDisposition = OobTargetDisposition.OuterHtml,
            IsEditing = true,
        });

        return Ok(tableModel);
    }

    /// <summary>
    /// Sets the value of a specific property on the item currently being edited.
    /// This action updates a single field value during the editing process.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being edited</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="propertyName">The name of the property to update</param>
    /// <param name="value">The new string value to set for the property</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result indicating success or failure of the value update</returns>
    [HttpPost("{typeId}/{modelUI}/SetValue")]
    public async Task<IActionResult> SetValue(string typeId, ModelUI modelUI, string propertyName, string? value, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SetValueImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            propertyName, value ?? string.Empty, scopedComponentId, modelHandler);
        return result!;
    }

    private async Task<IActionResult> SetValueImpl<T, TKey>(string propertyName, string? value, string componentId, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        var pageState = this.GetPageState();
        var formStatePartition = TableComponentIdentity.FormStatePartition(componentId);
        var editingExistingRecord = pageState.Get<bool>(formStatePartition, FormStateKeys.EditingExistingRecord)!;
        if (editingExistingRecord)
        {
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Update))
                return AuthorizationError(componentId);
        }
        else
        {
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Create))
                return AuthorizationError(componentId);
        }

        var editingItem = pageState.Get<T>(formStatePartition, FormStateKeys.EditingItem)!;
        var property = typeof(T).GetProperty(propertyName);
        if (property == null)
            return ValidationError("The submitted field could not be found.", componentId);

        try
        {
            var convertedValue = ConvertSubmittedValue(value, property.PropertyType);
            property.SetValue(editingItem, convertedValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set property {PropertyName} for model type {TypeId}.", propertyName, modelHandler.TypeId);
            return ValidationError("The submitted value is not valid for this field.", componentId);
        }

        pageState.Set(formStatePartition, FormStateKeys.EditingItem, editingItem);
        // We return a MultiSwapViewResult to allow the PageState to piggyback on the response
        return new MultiSwapViewResult();
    }

    private static object? ConvertSubmittedValue(string? value, Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (Nullable.GetUnderlyingType(propertyType) != null && string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (targetType == typeof(bool))
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                null or "" or "undefined" => false,
                "true" or "on" or "1" => true,
                "false" or "off" or "0" => false,
                _ => bool.Parse(value!)
            };
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value ?? string.Empty, ignoreCase: true);
        }

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    /// Handles value change events for input fields during editing.
    /// This action is triggered when a field value changes and may update related UI elements.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being edited</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="propertyName">The name of the property that changed</param>
    /// <param name="value">The new string value of the property</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result that may include UI updates based on the value change</returns>
    [HttpPost("{typeId}/{modelUI}/ValueChanged")]
    public async Task<IActionResult> ValueChanged(string typeId, ModelUI modelUI, string propertyName, string value, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(ValueChangedImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            propertyName, value, scopedComponentId, modelHandler);
        return result!;
    }

    private Task<IActionResult> ValueChangedImpl<T, TKey>(string propertyName, string value, string componentId,
        ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        throw new NotImplementedException("This method is not implemented yet.");
    }
}
