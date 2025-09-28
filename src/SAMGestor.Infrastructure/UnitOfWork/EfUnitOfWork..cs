using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.UnitOfWork
{
    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly SAMContext _ctx;
        private IDbContextTransaction? _currentTx;

        public EfUnitOfWork(SAMContext ctx) => _ctx = ctx;

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _ctx.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new UniqueConstraintViolationException("Unique constraint violation.", ex);
            }
        }

        public async Task BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken ct = default)
        {
            if (_currentTx != null) return;
            if (!_ctx.Database.IsRelational()) return;
            _currentTx = await _ctx.Database.BeginTransactionAsync(isolationLevel, ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (!_ctx.Database.IsRelational())
            {
                await SaveChangesAsync(ct); 
                return;
            }

            if (_currentTx == null) return;
            try
            {
                await SaveChangesAsync(ct); 
                await _currentTx.CommitAsync(ct);
            }
            finally
            {
                await _currentTx.DisposeAsync();
                _currentTx = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (!_ctx.Database.IsRelational()) return;
            if (_currentTx == null) return;
            try
            {
                await _currentTx.RollbackAsync(ct);
            }
            finally
            {
                await _currentTx.DisposeAsync();
                _currentTx = null;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            
            if (ex.InnerException is PostgresException pg &&
                pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }
            return false;
        }
    }
}
