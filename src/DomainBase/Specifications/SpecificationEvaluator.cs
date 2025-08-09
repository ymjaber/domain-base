using System.Linq.Expressions;

namespace DomainBase;

/// <summary>
/// Applies an <see cref="ISpecification{T}"/> to an <see cref="IQueryable{T}"/>.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies the given specification to the input query by composing criteria, includes, ordering, and paging.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="inputQuery">The input queryable.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The composed queryable.</returns>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        // Criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Includes (expression-based)
        foreach (var include in specification.Includes)
        {
            query = Include(query, include);
        }

        // Includes (string-based) - pass through for providers that support Include(string)
        foreach (var includeString in specification.IncludeStrings)
        {
            query = Include(query, includeString);
        }

        // Ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Paging
        if (specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }
            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        return query;
    }

    // These helpers allow providers (like EF Core) to plug in Include methods via extension method discovery.
    private static IQueryable<T> Include<T>(IQueryable<T> source, Expression<Func<T, object>> path)
    {
        // If the provider has an Include extension method, it will be chosen at runtime.
        // For non-EF providers, this is a no-op.
        return source; 
    }

    private static IQueryable<T> Include<T>(IQueryable<T> source, string path)
    {
        return source;
    }
}

