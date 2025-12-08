namespace SAMGestor.Application.Interfaces;

public interface IRawSqlExecutor
{
    Task<int> ExecuteSqlAsync(string sql, CancellationToken ct = default);
}