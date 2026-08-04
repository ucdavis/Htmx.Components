using System.Text.Json;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Table;
using Htmx.Components.Utilities;
using Microsoft.AspNetCore.Mvc;
using static Htmx.Components.Authorization.AuthConstants;
using static Htmx.Components.State.PageStateConstants;
using Htmx.Components.Table.Internal;

namespace Htmx.Components.Controllers;

public partial class FormController
{
    /// <summary>
    /// Saves the currently edited item, either creating a new record or updating an existing one.
    /// This action commits changes made during the editing process to the data store.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being saved</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result containing the updated table view or error information</returns>
    [HttpPost("{typeId}/{modelUI}/Save")]
    [TableEditAction]
    public async Task<IActionResult> Save(string typeId, ModelUI modelUI, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SaveImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            scopedComponentId, modelHandler);

        return result;
    }

    private async Task<IActionResult> SaveImpl<T, TKey>(string componentId, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        var pageState = this.GetPageState();
        var formStatePartition = TableComponentIdentity.FormStatePartition(componentId);
        var editingItem = pageState.Get<T>(formStatePartition, FormStateKeys.EditingItem);
        if (editingItem is null)
            return CrudError("Editing session expired. Refresh and try again.", componentId);

        var editingExistingRecord = pageState.Get<bool>(formStatePartition, FormStateKeys.EditingExistingRecord)!;
        var tableModel = await modelHandler.BuildTableModelAsync();
        tableModel.ComponentId = componentId;
        if (editingExistingRecord)
        {
            if (modelHandler.UpdateModel == null)
                return CrudError("This item cannot be updated.", componentId);

            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Update))
                return AuthorizationError(componentId);
            var result = await modelHandler.UpdateModel!(editingItem);
            if (result.IsError)
            {
                // If the update failed, we return the error message
                return CrudError(result.Message, componentId);
            }
            tableModel.Rows.Add(new TableRowContext<T, TKey>
            {
                Item = result.Value,
                ModelHandler = modelHandler,
                Key = modelHandler.KeySelectorFunc(result.Value),
                TargetDisposition = OobTargetDisposition.OuterHtml,
            });
        }
        else
        {
            if (modelHandler.CreateModel == null)
                return CrudError("This item cannot be created.", componentId);

            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Create))
                return AuthorizationError(componentId);
            var result = await modelHandler.CreateModel!(editingItem);
            if (result.IsError)
            {
                // If the creation failed, we return the error message
                return CrudError(result.Message, componentId);
            }
            tableModel.Rows.Add(new TableRowContext<T, TKey>
            {
                Item = null!,
                ModelHandler = modelHandler,
                StringKey = "new",
                TargetDisposition = OobTargetDisposition.Delete,
            });
            tableModel.Rows.Add(new TableRowContext<T, TKey>
            {
                Item = result.Value,
                ModelHandler = modelHandler,
                Key = modelHandler.KeySelectorFunc(result.Value),
                TargetDisposition = OobTargetDisposition.AfterBegin,
                TargetSelector = TableComponentIdentity.BodySelector(componentId),
            });
        }

        pageState.ClearKey(formStatePartition, FormStateKeys.EditingItem);
        pageState.ClearKey(formStatePartition, FormStateKeys.EditingExistingRecord);

        return Ok(tableModel);
    }

    /// <summary>
    /// Cancels the current editing operation and reverts to the display state.
    /// This action discards any unsaved changes and returns the item to read-only mode.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being edited</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result containing the updated table view without the editing UI</returns>
    [HttpPost("{typeId}/{modelUI}/CancelEdit")]
    [TableEditAction]
    public async Task<IActionResult> CancelEdit(string typeId, ModelUI modelUI, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(CancelEditImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            scopedComponentId, modelHandler);

        return result!;
    }

    private async Task<IActionResult> CancelEditImpl<T, TKey>(string componentId, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        var tableModel = await modelHandler.BuildTableModelAsync();
        tableModel.ComponentId = componentId;
        var pageState = this.GetPageState();
        var formStatePartition = TableComponentIdentity.FormStatePartition(componentId);
        var editingItem = pageState.Get<T>(formStatePartition, FormStateKeys.EditingItem);
        if (editingItem is null)
            return CrudError("Editing session expired. Refresh and try again.", componentId);

        if (pageState.Get<bool>(formStatePartition, FormStateKeys.EditingExistingRecord))
        {
            // Check if the user is authorized to read the item
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
                return AuthorizationError(componentId);

            var editingKey = modelHandler.KeySelectorFunc(editingItem);

            var query = await modelHandler.GetReadQueryAsync();
            var originalItem = await modelHandler.SingleAsync(query.Where(modelHandler.GetKeyPredicate(editingKey)));

            tableModel.Rows.Add(new TableRowContext<T, TKey>
            {
                Item = originalItem,
                ModelHandler = modelHandler,
                Key = editingKey,
                TargetDisposition = OobTargetDisposition.OuterHtml,
            });
        }
        else
        {
            tableModel.Rows.Add(new TableRowContext<T, TKey>
            {
                Item = null!,
                ModelHandler = modelHandler,
                StringKey = "new",
                TargetDisposition = OobTargetDisposition.Delete,
            });
        }

        pageState.ClearKey(formStatePartition, FormStateKeys.EditingItem);
        pageState.ClearKey(formStatePartition, FormStateKeys.EditingExistingRecord);
        return Ok(tableModel);
    }

    /// <summary>
    /// Initiates the creation of a new record by setting up an empty editing form.
    /// This action prepares the UI for creating a new item of the specified model type.
    /// </summary>
    /// <param name="typeId">The identifier of the model type to create</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result containing the creation form UI</returns>
    [HttpPost("{typeId}/{modelUI}/Create")]
    [TableEditAction]
    public async Task<IActionResult> Create(string typeId, ModelUI modelUI, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(CreateImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            scopedComponentId, modelHandler);
        return result!;
    }

    private async Task<IActionResult> CreateImpl<T, TKey>(string componentId, ModelHandler<T, TKey> modelHandler)
        where T : class, new()
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Create))
            return AuthorizationError(componentId);

        var editingItem = new T();

        var pageState = this.GetPageState();
        var formStatePartition = TableComponentIdentity.FormStatePartition(componentId);
        pageState.Set(formStatePartition, FormStateKeys.EditingItem, editingItem);
        pageState.Set(formStatePartition, FormStateKeys.EditingExistingRecord, false);

        var tableModel = await modelHandler.BuildTableModelAsync();
        tableModel.ComponentId = componentId;
        tableModel.Rows.Add(new TableRowContext<T, TKey>
        {
            Item = editingItem,
            ModelHandler = modelHandler,
            TargetDisposition = OobTargetDisposition.AfterBegin,
            TargetSelector = TableComponentIdentity.BodySelector(componentId),
            StringKey = "new",
            IsEditing = true,
        });

        return Ok(tableModel);
    }

    /// <summary>
    /// Deletes the specified record from the data store.
    /// This action permanently removes the item identified by the provided key.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being deleted</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="key">The unique key identifying the record to delete</param>
    /// <param name="componentId">The table component instance id that owns the request.</param>
    /// <returns>An action result indicating success or failure of the deletion operation</returns>
    [HttpPost("{typeId}/{modelUI}/Delete")]
    [TableEditAction]
    public async Task<IActionResult> Delete(string typeId, ModelUI modelUI, string key, string? componentId)
    {
        if (ValidateTableComponentId(componentId, out var scopedComponentId) is { } invalidComponent)
            return invalidComponent;

        var (resolvedModelHandler, error) = await ResolveModelHandler(typeId, modelUI, scopedComponentId);
        if (error is not null)
            return error;
        var modelHandler = resolvedModelHandler ?? throw new InvalidOperationException("Resolved model handler was null without an error result.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(DeleteImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            key, scopedComponentId, modelHandler);
        return result!;
    }

    private async Task<IActionResult> DeleteImpl<T, TKey>(string stringKey, string componentId, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (modelHandler.DeleteModel == null)
            return CrudError("This item cannot be deleted.", componentId);

        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Delete))
            return AuthorizationError(componentId);

        var key = (TKey)JsonSerializer.Deserialize(stringKey, modelHandler.KeyType)!;

        var result = await modelHandler.DeleteModel!(key);

        if (result.IsError)
        {
            // If the deletion failed, we return the error message
            return CrudError(result.Message, componentId);
        }

        var tableModel = await modelHandler.BuildTableModelAsync();
        tableModel.ComponentId = componentId;
        tableModel.Rows.Add(new TableRowContext<T, TKey>
        {
            Item = default!,
            ModelHandler = modelHandler,
            Key = key,
            TargetDisposition = OobTargetDisposition.Delete,
        });

        return Ok(tableModel);
    }
}
