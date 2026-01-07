namespace SAMGestor.Application.Common.Pagination;

public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    
    public int TotalCount { get; init; }
    
    public int Skip { get; init; }
    
    public int Take { get; init; }
    
    public bool HasNextPage => Skip + Items.Count < TotalCount;
    
    public bool HasPreviousPage => Skip > 0;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int skip, int take)
    {
        Items = items;
        TotalCount = totalCount;
        Skip = Math.Max(0, skip); 
        Take = take;
    }
}