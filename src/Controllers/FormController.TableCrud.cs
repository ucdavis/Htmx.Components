using System.Text.Json;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Table;
using Htmx.Components.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Htmx.Components.Authorization.AuthConstants;
using static Htmx.Components.State.PageStateConstants;

namespace Htmx.Components.Controllers;

public partial class FormController
{
    [HttpPost("{typeId}/{modelUI}/Save")]
    [TableEditAction]
    public async Task<IActionResult> Save(string typeId, ModelUI modelUI)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SaveImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            modelHandler);

        return result;
    }

    private async Task<IActionResult> SaveImpl<T, TKey>(ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (modelHandler.CreateModel == null)
            return BadRequest($"SaveModel not defined for type '{modelHandler.TypeId}'.");

        var pageState = this.GetPageState();
        var editingItem = pageState.Get<T>(FormStateKeys.Partition, FormStateKeys.EditingItem)!;
        var editingExistingRecord = pageState.Get<bool>(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord)!;
        var tableModel = await modelHandler.BuildTableModelAsync();
        if (editingExistingRecord)
        {
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Update))
                return Forbid();
            var result = await modelHandler.UpdateModel!(editingItem);
            if (result.IsError)
            {
                // If the update failed, we return the error message
                return BadRequest(result.Message);
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
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Create))
                return Forbid();
            var result = await modelHandler.CreateModel!(editingItem);
            if (result.IsError)
            {
                // If the creation failed, we return the error message
                return BadRequest(result.Message);
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
                TargetSelector = "#table-body",
            });
        }

        pageState.ClearKey(FormStateKeys.Partition, FormStateKeys.EditingItem);
        pageState.ClearKey(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord);

        return Ok(tableModel);
    }

    [HttpPost("{typeId}/{modelUI}/CancelEdit")]
    [TableEditAction]
    public async Task<IActionResult> CancelEdit(string typeId, ModelUI modelUI)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(CancelEditImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            modelHandler);

        return result!;
    }

    private async Task<IActionResult> CancelEditImpl<T, TKey>(ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        var tableModel = await modelHandler.BuildTableModelAsync();
        var pageState = this.GetPageState();
        if (pageState.Get<bool>(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord))
        {
            // Check if the user is authorized to read the item
            if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
                return Forbid();

            var editingItem = pageState.Get<T>(FormStateKeys.Partition, FormStateKeys.EditingItem)!;
            var editingKey = modelHandler.KeySelectorFunc(editingItem);

            var originalItem = await modelHandler.GetQueryable!()
                .Where(modelHandler.GetKeyPredicate(editingKey))
                .SingleAsync();

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

        pageState.ClearKey(FormStateKeys.Partition, FormStateKeys.EditingItem);
        pageState.ClearKey(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord);
        return Ok(tableModel);
    }

    [HttpPost("{typeId}/{modelUI}/Create")]
    [TableEditAction]
    public async Task<IActionResult> Create(string typeId, ModelUI modelUI)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(CreateImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            modelHandler);
        return result!;
    }

    private async Task<IActionResult> CreateImpl<T, TKey>(ModelHandler<T, TKey> modelHandler)
        where T : class, new()
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Create))
            return Forbid();

        var editingItem = new T();

        var pageState = this.GetPageState();
        pageState.Set(FormStateKeys.Partition, FormStateKeys.EditingItem, editingItem);
        pageState.Set(FormStateKeys.Partition, FormStateKeys.EditingExistingRecord, false);

        var tableModel = await modelHandler.BuildTableModelAsync();
        tableModel.Rows.Add(new TableRowContext<T, TKey>
        {
            Item = editingItem,
            ModelHandler = modelHandler,
            TargetDisposition = OobTargetDisposition.AfterBegin,
            TargetSelector = "#table-body",
            StringKey = "new",
            IsEditing = true,
        });

        return Ok(tableModel);
    }

    [HttpPost("{typeId}/{modelUI}/Delete")]
    [TableEditAction]
    public async Task<IActionResult> Delete(string typeId, ModelUI modelUI, string key)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, modelUI);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(DeleteImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            key, modelHandler);
        return result!;
    }

    private async Task<IActionResult> DeleteImpl<T, TKey>(string stringKey, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (modelHandler.DeleteModel == null)
            return BadRequest($"DeleteModel not defined for type '{modelHandler.TypeId}'.");

        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Delete))
            return Forbid();

        var key = (TKey)JsonSerializer.Deserialize(stringKey, modelHandler.KeyType)!;

        var result = await modelHandler.DeleteModel!(key);

        if (result.IsError)
        {
            // If the deletion failed, we return the error message
            return BadRequest(result.Message);
        }

        var pageState = this.GetPageState();
        var tableModel = await modelHandler.BuildTableModelAsync();
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