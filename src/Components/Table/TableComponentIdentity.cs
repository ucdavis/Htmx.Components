using System.Text.Json;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.State;
using Htmx.Components.Table.Models;

namespace Htmx.Components.Table;

#pragma warning disable CS1591

/// <summary>
/// Generates and resolves stable table component ids, DOM ids, request values, and state partitions.
/// </summary>
public static class TableComponentIdentity
{
    public const string ComponentIdField = "componentId";

    public static string Ensure(string? componentId = null)
    {
        if (string.IsNullOrWhiteSpace(componentId))
        {
            return "hc-table-" + Guid.NewGuid().ToString("N");
        }

        var sanitized = componentId.SanitizeForHtmlId();
        return sanitized.StartsWith("hc-table-", StringComparison.Ordinal)
            ? sanitized
            : "hc-table-" + sanitized;
    }

    public static string TableStatePartition(string componentId)
        => $"{PageStateConstants.TableStateKeys.Partition}:{Ensure(componentId)}";

    public static string FormStatePartition(string componentId)
        => $"{PageStateConstants.FormStateKeys.Partition}:{Ensure(componentId)}";

    public static string ContainerId(ITableModel table) => ElementId(table, "container");
    public static string BodyId(ITableModel table) => ElementId(table, "body");
    public static string HeaderId(ITableModel table) => ElementId(table, "header");
    public static string FilterRowId(ITableModel table) => ElementId(table, "filter-row");
    public static string PaginationId(ITableModel table) => ElementId(table, "pagination");
    public static string ActionListId(ITableModel table) => ElementId(table, "action-list");
    public static string EditToggleId(ITableModel table) => ElementId(table, "edit-toggle");
    public static string PageInputId(ITableModel table) => ElementId(table, "page");
    public static string PageSizeInputId(ITableModel table) => ElementId(table, "page-size");

    public static string BodySelector(string componentId) => "#" + ElementId(componentId, "body");

    public static string RowId(ITableModel table, ITableRowContext row)
        => ElementId(table.ComponentId, row.RowId);

    public static string FilterInputId(ITableColumnModel column, string? suffix = null)
    {
        var baseId = ElementId(column.Table.ComponentId, "filter-" + column.DataName);
        return string.IsNullOrWhiteSpace(suffix) ? baseId : $"{baseId}-{suffix.SanitizeForHtmlId()}";
    }

    public static string InputId(string componentId, string typeId, string inputId)
        => ElementId(componentId, $"{typeId}-{inputId}");

    public static string HxVals(string componentId, params (string Key, object? Value)[] values)
    {
        var hxVals = values.ToDictionary(value => value.Key, value => value.Value);
        hxVals[ComponentIdField] = Ensure(componentId);
        return JsonSerializer.Serialize(hxVals);
    }

    public static Dictionary<string, string> WithScopedHxVals(ActionModel action, string componentId)
    {
        var attributes = new Dictionary<string, string>(action.Attributes);
        var values = new Dictionary<string, object?>();

        if (attributes.TryGetValue("hx-vals", out var existingHxVals)
            && !string.IsNullOrWhiteSpace(existingHxVals))
        {
            values = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingHxVals)
                     ?? new Dictionary<string, object?>();
        }

        values[ComponentIdField] = Ensure(componentId);
        attributes["hx-vals"] = JsonSerializer.Serialize(values);
        return attributes;
    }

    private static string ElementId(ITableModel table, string name) => ElementId(table.ComponentId, name);

    private static string ElementId(string componentId, string name)
        => $"{Ensure(componentId)}-{name.SanitizeForHtmlId()}";
}

#pragma warning restore CS1591
