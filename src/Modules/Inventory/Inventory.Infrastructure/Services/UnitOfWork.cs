using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventory.Infrastructure.Services
{
    internal class UnitOfWork : IInventoryUnitOfWork, IDisposable
    {
        private readonly InventoryDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(InventoryDbContext context)
        {
            _context = context;
        }

        private int SaveChangesCoreSync()
        {
            try
            {
                return _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException(
                    "The stock record was modified by another operation while this request was running. Please retry the operation.",
                    ex);
            }
        }

        public int SaveChanges()
        {
            return SaveChangesCoreSync();
        }

        public void BeginTransaction()
        {
            if (_transaction != null)
                return;

            _transaction = _context.Database.BeginTransaction();
        }

        public async Task BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                return;

            _transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);
        }

        public void CommitTransaction()
        {
            try
            {
                _transaction?.Commit();
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task CommitTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (_transaction != null)
                    await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                if (_transaction != null)
                    await _transaction.DisposeAsync();

                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            try
            {
                _transaction?.Rollback();
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (_transaction != null)
                    await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                if (_transaction != null)
                    await _transaction.DisposeAsync();

                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            // Do not dispose _context — it is scoped and owned by DI container
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesCoreAsync(cancellationToken);
        }

        private async Task<int> SaveChangesCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // RowVersion optimistic-concurrency conflict: surface a clear retry message
                // instead of a raw database exception - stock is never silently corrupted.
                throw new InvalidOperationException(
                    "The stock record was modified by another operation while this request was running. Please retry the operation.",
                    ex);
            }
        }
    }
}
