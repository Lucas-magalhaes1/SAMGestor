namespace SAMGestor.Application.Common.Pagination;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int skip, int take)
    {
        if (skip > 0)
            query = query.Skip(skip);
        
        if (take > 0)
            query = query.Take(take);
        
        return query;
    }
    
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PagedQuery pagedQuery)
        => query.ApplyPagination(pagedQuery.Skip, pagedQuery.Take);
}