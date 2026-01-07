namespace SAMGestor.Application.Common.Pagination;
public abstract record PagedQuery
{
    private int _skip = 0;
    private int _take = 20;
    
    public int Skip
    {
        get => _skip;
        init => _skip = Math.Max(0, value);
    }
    
    public int Take
    {
        get => _take;
        init => _take = value < 0 ? 20 : Math.Min(value, 1000); 
    }
    
    public bool ReturnAll => _take == 0;
}