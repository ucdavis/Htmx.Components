using System.Data;
using System.Linq.Expressions;
using FastExpressionCompiler;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.ViewResults;
using Htmx.Components.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Htmx.Components.Table.Models;

namespace Htmx.Components.Table;

/// <summary>
/// Provides functionality for fetching and processing table data with Entity Framework Core support.
/// </summary>
/// <remarks>
/// The table provider handles pagination, filtering, sorting, and data retrieval for table models.
/// It works specifically with Entity Framework Core queryables and provides async operations
/// for optimal performance.
/// </remarks>
public interface ITableProvider
{
    /// <summary>
    /// Uses the given columns and tableState to extend the given queryable for appropriate
    /// filtering and sorting, and then executes the query twice; once with .CountAsync() so that
    /// PageCount can be calculated, and once with pagination applied. Places the results in the
    /// given <see cref="TableModel{T, TKey}"/>. The queryable is expected to be an EF Core queryable.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="tableModel">The table model to populate with data and metadata.</param>
    /// <param name="query">The Entity Framework Core queryable to execute.</param>
    /// <param name="tableState">The current state of the table including filters, sorting, and pagination.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method applies filtering, sorting, and pagination to the query in sequence,
    /// then executes both a count query and a data query to populate the table model
    /// with the appropriate page of data and total page count.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no sortable column is found for default sorting.</exception>
    Task FetchPageAsync<T, TKey>(
        TableModel<T, TKey> tableModel,
        IQueryable<T> query,
        TableState tableState)
        where T : class;
}

/// <summary>
/// Default implementation of <see cref="ITableProvider"/> that provides table data processing capabilities.
/// </summary>
/// <remarks>
/// This implementation uses Entity Framework Core for data access and provides filtering,
/// sorting, and pagination functionality. It integrates with the page state system to
/// maintain table state across requests.
/// </remarks>
public class TableProvider : ITableProvider
{
    private readonly IPageState _pageState;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableProvider"/> class.
    /// </summary>
    /// <param name="pageState">The page state service for maintaining table state across requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pageState"/> is null.</exception>
    public TableProvider(IPageState pageState)
    {
        _pageState = pageState ?? throw new ArgumentNullException(nameof(pageState));
    }

    /// <summary>
    /// Uses the given columns and tableState to extend the given queryable for appropriate
    /// filtering and sorting, and then executes the query twice; once with .CountAsync() so that
    /// PageCount can be calculated, and once with pagination applied. Places the results in the
    /// given <see cref="TableModel{T, TKey}"/>. The queryable is expected to be an EF Core queryable.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="tableModel">The table model to populate with data and metadata.</param>
    /// <param name="query">The Entity Framework Core queryable to execute.</param>
    /// <param name="tableState">The current state of the table including filters, sorting, and pagination.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// The method processes the query in the following order:
    /// 1. Applies text-based filtering using column filter delegates
    /// 2. Applies range-based filtering for date/numeric ranges
    /// 3. Applies sorting based on the current sort column and direction
    /// 4. Executes a count query to determine total records and page count
    /// 5. Applies pagination and executes the data query
    /// 6. Populates the table model with rows and metadata
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no sortable column is found for default sorting.</exception>
    public async Task FetchPageAsync<T, TKey>(
        TableModel<T, TKey> tableModel,
        IQueryable<T> query,
        TableState tableState)
        where T : class
    {
        if (tableModel == null) throw new ArgumentNullException(nameof(tableModel));
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (tableState == null) throw new ArgumentNullException(nameof(tableState));

        query = ApplyFiltering(query, tableState, tableModel);
        query = ApplyRangeFiltering(query, tableState, tableModel);
        query = ApplySorting(query, tableState, tableModel);

        var totalCount = await query.CountAsync();
        var pageCount = (int)Math.Ceiling((double)totalCount / tableState.PageSize);
        // make sure we're not trying to exceed the available pages
        tableState.Page = Math.Min(tableState.Page, pageCount);
        var pagedData = await query
            .Skip(Math.Max(tableState.Page - 1, 0) * tableState.PageSize)
            .Take(tableState.PageSize)
            .ToListAsync();

        var keySelector = tableModel.KeySelector?.CompileFast();

        tableModel.State = tableState;
        tableModel.PageCount = pageCount;
        tableModel.Rows = pagedData.Select((item, index) =>
        {
            var key = keySelector != null ? keySelector(item) : default;
            var rowContext = new TableRowContext<T, TKey>
            {
                Item = item,
                ModelHandler = tableModel.ModelHandler,
                PageIndex = tableState.Page - 1,
                Key = key
            };
            return rowContext;
        }).ToList();        
    }

    /// <summary>
    /// Applies range-based filtering to the queryable using configured range filter delegates.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="queryable">The queryable to apply filters to.</param>
    /// <param name="tableState">The table state containing range filter values.</param>
    /// <param name="tableModel">The table model containing column configurations.</param>
    /// <returns>The filtered queryable.</returns>
    /// <remarks>
    /// Range filters are typically used for date ranges or numeric ranges where users
    /// specify both minimum and maximum values. Only filters with both min and max
    /// values specified are applied.
    /// </remarks>
    private IQueryable<T> ApplyRangeFiltering<T, TKey>(IQueryable<T> queryable, TableState tableState, TableModel<T, TKey> tableModel)
        where T : class
    {
        // Find and pass any query param range filter values to their corresponding column range filter delegate
        // to apply it to the query
        if (tableState.RangeFilters != null)
        {
            foreach (var rangeFilter in tableState.RangeFilters.Where(f => !string.IsNullOrWhiteSpace(f.Value.Min)
                    && !string.IsNullOrWhiteSpace(f.Value.Max)))
            {
                var column = tableModel.Columns.FirstOrDefault(c => c.Header == rangeFilter.Key);
                if (column?.RangeFilter != null)
                {
                    queryable = column.RangeFilter(queryable, rangeFilter.Value.Min, rangeFilter.Value.Max);
                }
            }
        }

        return queryable;
    }

    /// <summary>
    /// Applies text-based filtering to the queryable using configured filter delegates.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="query">The queryable to apply filters to.</param>
    /// <param name="tableState">The table state containing filter values.</param>
    /// <param name="tableModel">The table model containing column configurations.</param>
    /// <returns>The filtered queryable.</returns>
    /// <remarks>
    /// Text filters are applied to columns that have filter delegates configured.
    /// Only non-empty filter values are processed.
    /// </remarks>
    private IQueryable<T> ApplyFiltering<T, TKey>(IQueryable<T> query, TableState tableState, TableModel<T, TKey> tableModel)
        where T: class
    {
        // Find and pass any tableState filter values to their corresponding column filter delegate
        // to apply it to the query
        if (tableState.Filters != null)
        {
            foreach (var filter in tableState.Filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
            {
                var column = tableModel.Columns.FirstOrDefault(c => c.DataName == filter.Key);
                if (column?.Filter != null)
                {
                    query = column.Filter(query, filter.Value);
                }
            }
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the queryable based on the current sort configuration.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <typeparam name="TKey">The key type for the entity.</typeparam>
    /// <param name="query">The queryable to apply sorting to.</param>
    /// <param name="tableState">The table state containing sort column and direction.</param>
    /// <param name="tableModel">The table model containing column configurations.</param>
    /// <returns>The sorted queryable.</returns>
    /// <remarks>
    /// Sorting is required for consistent pagination. If no sort column is specified,
    /// the method defaults to sorting by the first column with a selector expression.
    /// Both ascending and descending sort directions are supported.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no sortable column is found for default sorting.</exception>
    private IQueryable<T> ApplySorting<T, TKey>(IQueryable<T> query, TableState tableState, TableModel<T, TKey> tableModel)
        where T : class
    {
        // In order for pagination to be consistent, we need to always define a sort.
        if (!string.IsNullOrEmpty(tableState.SortColumn))
        {
            var column = tableModel.Columns.FirstOrDefault(c => c.Header == tableState.SortColumn);
            if (column != null)
            {
                query = tableState.SortDirection == "asc"
                    ? query.OrderBy(column.SelectorExpression)
                    : query.OrderByDescending(column.SelectorExpression);
            }
        }
        else
        {
            // Since no sort is specified, we'll just sort by the first selector column
            var column = tableModel.Columns.FirstOrDefault(c => c.SelectorExpression != null);
            if (column == null)
                throw new InvalidOperationException("No selector column found for default sorting.");
            query = query.OrderBy(column.SelectorExpression);
        }

        return query;
    }
}