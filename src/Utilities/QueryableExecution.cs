using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Htmx.Components.Utilities;

internal static class QueryableExecution
{
    public static Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.Provider is IAsyncQueryProvider
            ? EntityFrameworkQueryableExtensions.CountAsync(query, cancellationToken)
            : Task.FromResult(query.Count());

    public static Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.Provider is IAsyncQueryProvider
            ? EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken)
            : Task.FromResult(query.ToList());

    public static Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.Provider is IAsyncQueryProvider
            ? EntityFrameworkQueryableExtensions.SingleAsync(query, cancellationToken)
            : Task.FromResult(query.Single());

    public static Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.Provider is IAsyncQueryProvider
            ? EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(query, cancellationToken)
            : Task.FromResult(query.SingleOrDefault());
}
