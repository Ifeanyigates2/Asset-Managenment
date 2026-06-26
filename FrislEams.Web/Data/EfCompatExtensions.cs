using System.Linq.Expressions;

namespace FrislEams.Web.Data;

public static class EfCompatExtensions
{
    public static IQueryable<T> Include<T, TProperty>(this IQueryable<T> source, Expression<Func<T, TProperty>> navigationPropertyPath)
        => source;

    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        => Task.FromResult(source.ToList());

    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        => Task.FromResult(source.FirstOrDefault());

    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(source.FirstOrDefault(predicate.Compile()));

    public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        => Task.FromResult(source.Count());

    public static Task<int> CountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(source.Count(predicate.Compile()));

    public static Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(source.Any(predicate.Compile()));

    public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<T, TKey, TElement>(
        this IQueryable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken = default) where TKey : notnull
        => Task.FromResult(source.ToDictionary(keySelector, elementSelector));
}
