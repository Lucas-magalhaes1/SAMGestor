using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.UnitOfWork
{
    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly SAMContext _ctx;
        private IDbContextTransaction? _currentTx;

        public EfUnitOfWork(SAMContext ctx) => _ctx = ctx;

        public Task SaveChangesAsync(CancellationToken ct = default)
            => _ctx.SaveChangesAsync(ct);

        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default)
        {
            if (_currentTx != null) return;

            // InMemory NÃO é relacional -> não abre transação
            if (!_ctx.Database.IsRelational())
                return;

            _currentTx = await _ctx.Database.BeginTransactionAsync(isolationLevel, ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            // Se não relacional, só salva e sai
            if (!_ctx.Database.IsRelational())
            {
                await _ctx.SaveChangesAsync(ct);
                return;
            }

            if (_currentTx == null) return;
            try
            {
                await _ctx.SaveChangesAsync(ct);
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
            if (!_ctx.Database.IsRelational())
                return;

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
    }
}
