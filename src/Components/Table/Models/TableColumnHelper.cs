using System.Globalization;
using System.Linq.Dynamic.Core;
using Htmx.Components.Extensions;
using Serilog;

namespace Htmx.Components.Table.Models;

/// <summary>
/// Provides utility methods for table column operations including filtering and value parsing.
/// </summary>
/// <remarks>
/// This static class contains methods for applying dynamic filters to queryables based on
/// string filter expressions. It supports various operators and data types commonly used
/// in table filtering scenarios.
/// </remarks>
public static class TableColumnHelper
{
    /// <summary>
    /// Applies a filter string to the queryable using column metadata from the table model.
    /// </summary>
    /// <typeparam name="T">The entity type being filtered.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="query">The queryable to apply the filter to.</param>
    /// <param name="filter">The filter expression string.</param>
    /// <param name="column">The column model containing metadata for filtering.</param>
    /// <returns>A filtered queryable with the specified filter applied.</returns>
    /// <remarks>
    /// <para>
    /// Filter syntax: [Operator] [Operand1], [Operand2]
    /// </para>
    /// <para>
    /// Supported operators:
    /// - Comparison: =, ==, !=, &lt;, &gt;, &lt;=, &gt;=
    /// - String operations: contains, startswith, endswith
    /// - Null checks: isnull, isnotnull, isnullorempty, isnotnullorempty
    /// - Range: between [value1], [value2]
    /// </para>
    /// <para>
    /// Operands can be:
    /// - Quoted strings: "some value"
    /// - Unquoted values: 123, true, 2023-01-01
    /// - Column references: [Other Column Name]
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Filter for names containing "John"
    /// var filtered = TableColumnHelper.Filter(query, "contains John", nameColumn);
    /// 
    /// // Filter for ages between 18 and 65
    /// var filtered = TableColumnHelper.Filter(query, "between 18, 65", ageColumn);
    /// 
    /// // Filter for dates after a specific date
    /// var filtered = TableColumnHelper.Filter(query, "> 2023-01-01", dateColumn);
    /// </code>
    /// </example>
    public static IQueryable<T> Filter<T, TKey>(IQueryable<T> query, string filter, TableColumnModel<T, TKey> column)
        where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var tableModel = (TableModel<T, TKey>)column.Table ?? throw new InvalidOperationException("Column Table is not set.");
            var propertyName = column.DataName ?? throw new InvalidOperationException("Column DataName is not set.");
            var propertyType = column.SelectorExpression?.GetMemberType() ?? throw new InvalidOperationException("Column SelectorExpression is not set.");

            // Parse filter string
            var (op, operands) = ParseFilter(filter);

            string dynamicQuery = "";
            object?[] values = Array.Empty<object?>();

            // Operator logic
            switch (op.ToLowerInvariant())
            {
                case "isnull":
                    dynamicQuery = $"{propertyName} == null";
                    break;
                case "isnotnull":
                    dynamicQuery = $"{propertyName} != null";
                    break;
                case "isnullorempty":
                    if (propertyType == typeof(string))
                        dynamicQuery = $"string.IsNullOrEmpty({propertyName})";
                    break;
                case "isnotnullorempty":
                    if (propertyType == typeof(string))
                        dynamicQuery = $"!string.IsNullOrEmpty({propertyName})";
                    break;
                case "between":
                    if (operands.Count == 2)
                    {
                        if (IsColumnReference(operands[0], tableModel) && IsColumnReference(operands[1], tableModel))
                        {
                            var col0 = GetColName(tableModel, operands[0]);
                            var col1 = GetColName(tableModel, operands[1]);
                            dynamicQuery = $"{propertyName} >= {col0} && {propertyName} <= {col1}";
                        }
                        else if (IsColumnReference(operands[0], tableModel))
                        {
                            var col0 = GetColName(tableModel, operands[0]);
                            dynamicQuery = $"{propertyName} >= {col0} && {propertyName} <= @0";
                            values = [ParseValue(operands[1], propertyType)];
                        }
                        else if (IsColumnReference(operands[1], tableModel))
                        {
                            var col1 = GetColName(tableModel, operands[1]);
                            dynamicQuery = $"{propertyName} >= @0 && {propertyName} <= {col1}";
                            values = [ParseValue(operands[0], propertyType)];
                        }
                        else
                        {
                            dynamicQuery = $"{propertyName} >= @0 && {propertyName} <= @1";
                            values = [ParseValue(operands[0], propertyType), ParseValue(operands[1], propertyType)];
                        }
                    }
                    break;
                case ">":
                case "<":
                case ">=":
                case "<=":
                case "!=":
                case "=":
                case "==":
                    if (operands.Count == 1)
                    {
                        if (IsColumnReference(operands[0], tableModel))
                        {
                            var otherColName = GetColName(tableModel, operands[0]);
                            dynamicQuery = $"{propertyName} {op} {otherColName}";
                        }
                        else
                        {
                            dynamicQuery = $"{propertyName} {op} @0";
                            values = [ParseValue(operands[0], propertyType)];
                        }
                    }
                    break;
                case "contains":
                    if (propertyType == typeof(string) && operands.Count == 1)
                    {
                        if (IsColumnReference(operands[0], tableModel))
                        {
                            var otherColName = GetColName(tableModel, operands[0]);
                            dynamicQuery = $"{propertyName} != null && {propertyName}.Contains({otherColName})";
                        }
                        else
                        {
                            dynamicQuery = $"{propertyName} != null && {propertyName}.Contains(@0)";
                            values = [ParseValue(operands[0], propertyType)];
                        }
                    }
                    break;
                case "startswith":
                    if (propertyType == typeof(string) && operands.Count == 1)
                    {
                        if (IsColumnReference(operands[0], tableModel))
                        {
                            var otherColName = GetColName(tableModel, operands[0]);
                            dynamicQuery = $"{propertyName} != null && {propertyName}.StartsWith({otherColName})";
                        }
                        else
                        {
                            dynamicQuery = $"{propertyName} != null && {propertyName}.StartsWith(@0)";
                            values = [ParseValue(operands[0], propertyType)];
                        }
                    }
                    break;
                case "endswith":
                    if (propertyType == typeof(string) && operands.Count == 1)
                    {
                        if (IsColumnReference(operands[0], tableModel))
                        {
                            var otherColName = GetColName(tableModel, operands[0]);
                            dynamicQuery = $"{propertyName} != null && {propertyName}.EndsWith({otherColName})";
                        }
                        else
                        {
                            dynamicQuery = $"{propertyName} != null && {propertyName}.EndsWith(@0)";
                            values = [ParseValue(operands[0], propertyType)];
                        }
                    }
                    break;
                default:
                    // Fallback: treat as "contains" for string columns
                    if (propertyType == typeof(string))
                    {
                        dynamicQuery = $"{propertyName} != null && {propertyName}.Contains(@0)";
                        values = [ParseValue(filter.Trim(), propertyType)];
                    }
                    break;
            }

            if (string.IsNullOrEmpty(dynamicQuery))
                return query;

            return values.Length > 0
                ? query.Where(dynamicQuery, values)
                : query.Where(dynamicQuery);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply filter '{Filter}' on column '{ColumnName}' of table for type '{TypeId}'.", filter, column.DataName, column.Table?.TypeId);
            return query;
        }
    }

    /// <summary>
    /// Gets the column name from a table model based on the operand reference.
    /// </summary>
    /// <typeparam name="T">The entity type being filtered.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="tableModel">The table model containing column metadata.</param>
    /// <param name="operand">The operand reference to resolve to a column name.</param>
    /// <returns>The resolved column name.</returns>
    private static string GetColName<T, TKey>(TableModel<T, TKey> tableModel, string operand) where T : class
    {
        var header = operand.Trim('[', ']');
        var otherCol = tableModel.Columns.FirstOrDefault(c =>
            string.Equals(c.Header, header, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.DataName, header, StringComparison.OrdinalIgnoreCase));
        var otherColName = otherCol?.DataName ?? otherCol?.Header ?? throw new InvalidOperationException("Column not found");
        return otherColName;
    }

    /// <summary>
    /// Parses a filter string into operator and operands.
    /// </summary>
    /// <param name="filter">The filter string to parse.</param>
    /// <returns>A tuple containing the operator and list of operands.</returns>
    /// <remarks>
    /// Handles quoted strings, bracketed column references, and comma-separated values.
    /// Supports incomplete quotes and brackets for robustness.
    /// </remarks>
    private static (string op, List<string> operands) ParseFilter(string filter)
    {
        // Split on first whitespace for operator, then parse operands (comma-separated, quoted, or bracketed)
        var firstSpace = filter.IndexOf(' ');
        string op, rest;
        if (firstSpace == -1)
        {
            op = filter.Trim();
            rest = "";
        }
        else
        {
            op = filter.Substring(0, firstSpace).Trim();
            rest = filter.Substring(firstSpace + 1).Trim();
        }

        var operands = new List<string>();
        if (!string.IsNullOrEmpty(rest))
        {
            int i = 0;
            while (i < rest.Length)
            {
                // Skip leading whitespace and commas
                while (i < rest.Length && (rest[i] == ',' || char.IsWhiteSpace(rest[i]))) i++;
                if (i >= rest.Length) break;

                if (rest[i] == '"')
                {
                    int start = i + 1;
                    int end = rest.IndexOf('"', start);
                    if (end == -1) end = rest.Length; // Incomplete quote: take to end
                    operands.Add(rest.Substring(start, end - start).Trim());
                    i = end < rest.Length ? end + 1 : rest.Length;
                }
                else if (rest[i] == '[')
                {
                    int start = i;
                    int end = rest.IndexOf(']', start + 1);
                    if (end == -1) end = rest.Length - 1; // Incomplete bracket: take to end
                    operands.Add(rest.Substring(start, end - start + 1).Trim());
                    i = end < rest.Length ? end + 1 : rest.Length;
                }
                else
                {
                    int start = i;
                    int end = rest.IndexOf(',', start);
                    if (end == -1) end = rest.Length;
                    operands.Add(rest.Substring(start, end - start).Trim());
                    i = end;
                }
            }
        }
        // Remove empty operands
        operands = operands.Where(o => !string.IsNullOrEmpty(o)).ToList();
        return (op, operands);
    }

    /// <summary>
    /// Determines if a string is surrounded by brackets, indicating a column reference.
    /// </summary>
    /// <param name="s">The string to check.</param>
    /// <returns>true if the string starts with '[' and ends with ']'; otherwise, false.</returns>
    private static bool IsBracketed(string s) => s.StartsWith("[") && s.EndsWith("]");

    /// <summary>
    /// Determines if an operand refers to another column in the table.
    /// </summary>
    /// <param name="operand">The operand to check.</param>
    /// <param name="tableModel">The table model to search for column references.</param>
    /// <returns>true if the operand is a valid column reference; otherwise, false.</returns>
    private static bool IsColumnReference(string operand, ITableModel tableModel)
    {
        if (!IsBracketed(operand)) return false;
        var header = operand.Trim('[', ']');
        return tableModel.Columns.Any(c =>
            string.Equals(c.Header, header, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.DataName, header, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parses a string value to the specified type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="type">The target type to parse to.</param>
    /// <returns>The parsed value, or null if parsing fails.</returns>
    private static object? ParseValue(string value, Type type)
    {
        if (type == typeof(string))
            return value;
        if (type == typeof(int) || type == typeof(int?))
            return int.TryParse(value, out var i) ? i : null;
        if (type == typeof(long) || type == typeof(long?))
            return long.TryParse(value, out var l) ? l : null;
        if (type == typeof(double) || type == typeof(double?))
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        if (type == typeof(decimal) || type == typeof(decimal?))
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var m) ? m : null;
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
        if (type == typeof(bool) || type == typeof(bool?))
            return bool.TryParse(value, out var b) ? b : null;
        if (type.IsEnum)
            return Enum.TryParse(type, value, true, out var e) ? e : null;
        return value;
    }
}
