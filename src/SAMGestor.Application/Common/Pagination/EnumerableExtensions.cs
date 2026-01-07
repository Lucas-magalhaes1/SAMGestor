namespace SAMGestor.Application.Common.Pagination;

public static class EnumerableExtensions
{
    
    public static IEnumerable<T> ApplyPagination<T>(this IEnumerable<T> source, int skip, int take)
    {
        if (skip > 0)
            source = source.Skip(skip);

        if (take > 0)
            source = source.Take(take);

        return source;
    }

    /// <summary>
    /// Cria um PagedResult de uma coleção em memória.
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(
        this IEnumerable<T> source,
        int skip,
        int take)
    {
        var list = source as IReadOnlyList<T> ?? source.ToList();
        var totalCount = list.Count;

        var items = list
            .ApplyPagination(skip, take)
            .ToList();

        return new PagedResult<T>(items, totalCount, skip, take);
    }
}