using System.Text.Json;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Table;
using Htmx.Components.Utilities;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Htmx.Components.Authorization.AuthConstants;
using static Htmx.Components.State.PageStateConstants;

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
    /// <returns>An action result containing the edit form UI</returns>
    [HttpPost("{typeId}/{modelUI}/Edit")]
    [TableEditAction]
    public async Task<IActionResult> Edit(string typeId, ModelUI modelUI, string key)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(EditImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            key, modelHandler);
        return result!;
    }

    private async Task<IActionResult> EditImpl<T, TKey>(string stringKey, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
            return Forbid();
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Update))
            return Forbid();

        var key = (TKey)JsonSerializer.Deserialize(stringKey, modelHandler.KeyType)!;
        var editingItem = await modelHandler.GetQueryable!()
            .Where(modelHandler.GetKeyPredicate(key))
            .SingleOrDefaultAsync();
        if (editingItem == null)
            return BadRequest($"Model with key '{stringKey}' not found.");
        var pageState = this.GetPageState();
        pageState.Set(FormStateKeys.Partition, FormStateKeys.EditingItem, editingItem);
        pageState.Set(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord, true);

        var tableModel = await modelHandler.BuildTableModelAsync();
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
    /// <returns>An action result indicating success or failure of the value update</returns>
    [HttpPost("{typeId}/{modelUI}/SetValue")]
    public async Task<IActionResult> SetValue(string typeId, ModelUI modelUI, string propertyName, string value)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SetValueImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            propertyName, value, modelHandler);
        return result!;
    }

    private async Task<IActionResult> SetValueImpl<T, TKey>(string propertyName, string value, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        var pageState = this.GetPageState();
        var editingExistingRecord = pageState.Get<bool>(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord)!;
        if (editingExistingRecord)
        {
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Update))
                return Forbid();
        }
        else
        {
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Create))
                return Forbid();
        }

        var tableState = pageState.GetOrCreate<TableState>(TableStateKeys.Partition, TableStateKeys.TableState, () => new());
        var editingItem = pageState.Get<T>(FormStateKeys.Partition, FormStateKeys.EditingItem)!;
        var property = typeof(T).GetProperty(propertyName);
        if (property == null)
            return BadRequest($"Property '{propertyName}' not found.");

        try
        {
            var convertedValue = Convert.ChangeType(value, property.PropertyType);
            property.SetValue(editingItem, convertedValue);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to set property '{propertyName}': {ex.Message}");
        }

        pageState.Set(FormStateKeys.Partition, FormStateKeys.EditingItem, editingItem);
        // We return a MultiSwapViewResult to allow the PageState to piggyback on the response
        return new MultiSwapViewResult();
    }

    /// <summary>
    /// Handles value change events for input fields during editing.
    /// This action is triggered when a field value changes and may update related UI elements.
    /// </summary>
    /// <param name="typeId">The identifier of the model type being edited</param>
    /// <param name="modelUI">The UI context (typically Table) for the operation</param>
    /// <param name="propertyName">The name of the property that changed</param>
    /// <param name="value">The new string value of the property</param>
    /// <returns>An action result that may include UI updates based on the value change</returns>
    [HttpPost("{typeId}/{modelUI}/ValueChanged")]
    public async Task<IActionResult> ValueChanged(string typeId, ModelUI modelUI, string propertyName, string value)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(ValueChangedImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            propertyName, value, modelHandler);
        return result!;
    }

    private Task<IActionResult> ValueChangedImpl<T, TKey>(string propertyName, string value,
        ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        throw new NotImplementedException("This method is not implemented yet.");
    }
}