using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Interfaces;

namespace SAMGestor.Infrastructure.Persistence;

public class RawSqlExecutor : IRawSqlExecutor
{
    private readonly SAMContext _context;

    public RawSqlExecutor(SAMContext context)
    {
        _context = context;
    }

    public async Task<int> ExecuteSqlAsync(string sql, CancellationToken ct = default)
    {
        return await _context.Database.ExecuteSqlRawAsync(sql, ct);
    }
}