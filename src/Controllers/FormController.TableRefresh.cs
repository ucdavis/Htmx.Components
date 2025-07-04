using Htmx.Components.Table;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.Utilities;
using Microsoft.AspNetCore.Mvc;
using static Htmx.Components.Authorization.AuthConstants;
using static Htmx.Components.State.PageStateConstants;

namespace Htmx.Components.Controllers;

public partial class FormController
{
    [HttpPost("{typeId}/SetPage")]
    [TableRefreshAction]
    public async Task<IActionResult> SetPage(string typeId, int page)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, ModelUI.Table);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SetPageImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            page, modelHandler);
        return result!;
    }

    private async Task<IActionResult> SetPageImpl<T, TKey>(int page, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
            return Forbid();

        var pageState = this.GetPageState();
        var tableState = pageState.GetOrCreate<TableState>(TableStateKeys.Partition, TableStateKeys.TableState, () => new());
        tableState.Page = page;
        pageState.Set(TableStateKeys.Partition, TableStateKeys.TableState, tableState);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync(tableState);

        return Ok(tableModel);
    }

    [HttpPost("{typeId}/SetPageSize")]
    [TableRefreshAction]
    public async Task<IActionResult> SetPageSize(string typeId, int pageSize)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, ModelUI.Table);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SetPageSizeImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            pageSize, modelHandler);
        return result!;
    }

    private async Task<IActionResult> SetPageSizeImpl<T, TKey>(int pageSize, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
            return Forbid();

        var pageState = this.GetPageState();
        var tableState = pageState.GetOrCreate<TableState>(TableStateKeys.Partition, TableStateKeys.TableState, () => new());
        tableState.PageSize = pageSize;
        pageState.Set(TableStateKeys.Partition, TableStateKeys.TableState, tableState);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync(tableState);

        return Ok(tableModel);
    }

    [HttpPost("{typeId}/SetSort")]
    [TableRefreshAction]
    public async Task<IActionResult> SetSort(string typeId, string column, string direction)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, ModelUI.Table);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SetSortImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            column, direction, modelHandler);
        return result!;
    }

    private async Task<IActionResult> SetSortImpl<T, TKey>(string column, string direction, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
            return Forbid();

        var pageState = this.GetPageState();
        var tableState = pageState.GetOrCreate<TableState>(TableStateKeys.Partition, TableStateKeys.TableState, () => new());
        tableState.SortColumn = column;
        tableState.SortDirection = direction;
        pageState.Set(TableStateKeys.Partition, TableStateKeys.TableState, tableState);
        var tableModel = await modelHandler.BuildTableModelAndFetchPageAsync(tableState);

        return Ok(tableModel);
    }

    [HttpPost("{typeId}/SetFilter")]
    [TableRefreshAction]
    public async Task<IActionResult> SetFilter(string typeId, string column, string filter, int input)
    {
        var modelHandler = await _modelRegistry.GetModelHandler(typeId, ModelUI.Table);
        if (modelHandler == null)
            return BadRequest($"Model handler for type '{typeId}' not found.");

        var result = await GenericMethodInvoker.InvokeAsync<IActionResult>(
            this,
            nameof(SetFilterImpl),
            [modelHandler.ModelType, modelHandler.KeyType],
            column, filter, input, modelHandler);
        return result!;
    }

    private async Task<IActionResult> SetFilterImpl<T, TKey>(string column, string filter, int input, ModelHandler<T, TKey> modelHandler)
        where T : class
    {
        if (!await IsAuthorized(modelHandler.TypeId, CrudOperations.Read))
            return Forbid();

        var tableModel = await modelHandler.BuildTableModelAsync();
        var columnModel = tableModel.Columns.FirstOrDefault(c => c.DataName == column);
        if (columnModel == null)
            return BadRequest($"Column '{column}' not found.");

        if (!columnModel.Filterable || (columnModel.RangeFilter == null && columnModel.Filter == null))
            return BadRequest($"Column '{column}' is not filterable.");

        var pageState = this.GetPageState();
        var tableState = pageState.GetOrCreate<TableState>(TableStateKeys.Partition, TableStateKeys.TableState, () => new());
        if (columnModel.Filter != null)
        {
            if (string.IsNullOrEmpty(filter))
                tableState.Filters.Remove(column);
            else
                tableState.Filters[column] = filter;
        }
        else if (columnModel.RangeFilter != null)
        {
            (var from, var to) = tableState.RangeFilters.TryGetValue(column, out var range) ? range : ("", "");
            if (input == 1)
                from = filter;
            else if (input == 2)
                to = filter;
            else
                return BadRequest($"Invalid input value: {input}");
            tableState.RangeFilters[column] = (from, to);
        }

        pageState.Set(TableStateKeys.Partition, TableStateKeys.TableState, tableState);
        await _tableProvider.FetchPageAsync(tableModel, modelHandler.GetQueryable!(), tableState);
        return Ok(tableModel);
    }
}