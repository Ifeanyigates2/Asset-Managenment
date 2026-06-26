using System.Linq.Expressions;

namespace FrislEams.Web.Data;

public static class MongoEntitySetExtensions
{
    public static IQueryable<T> Include<T, TProperty>(this MongoEntitySet<T> source, Expression<Func<T, TProperty>> navigationPropertyPath) where T : class
        => source.AsQueryable().Include(navigationPropertyPath);

    public static IQueryable<T> Where<T>(this MongoEntitySet<T> source, Expression<Func<T, bool>> predicate) where T : class
        => source.AsQueryable().Where(predicate);

    public static IOrderedQueryable<T> OrderByDescending<T, TKey>(this MongoEntitySet<T> source, Expression<Func<T, TKey>> keySelector) where T : class
        => source.AsQueryable().OrderByDescending(keySelector);

    public static IOrderedQueryable<T> OrderBy<T, TKey>(this MongoEntitySet<T> source, Expression<Func<T, TKey>> keySelector) where T : class
        => source.AsQueryable().OrderBy(keySelector);

    public static Task<T?> FirstOrDefaultAsync<T>(this MongoEntitySet<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
        => source.AsQueryable().FirstOrDefaultAsync(predicate, cancellationToken);

    public static Task<T?> FirstOrDefaultAsync<T>(this MongoEntitySet<T> source, CancellationToken cancellationToken = default) where T : class
        => source.AsQueryable().FirstOrDefaultAsync(cancellationToken);

    public static Task<bool> AnyAsync<T>(this MongoEntitySet<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
        => source.AsQueryable().AnyAsync(predicate, cancellationToken);

    public static Task<int> CountAsync<T>(this MongoEntitySet<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
        => source.AsQueryable().CountAsync(predicate, cancellationToken);

    public static Task<int> CountAsync<T>(this MongoEntitySet<T> source, CancellationToken cancellationToken = default) where T : class
        => source.AsQueryable().CountAsync(cancellationToken);
}
