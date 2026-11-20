using Identity.Application.Common.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Infrastructure.Services
{
    internal class UnitOfWork(AppIdentityDbContext context) : IIdentityUnitOfWork, IDisposable
    {
        private IDbContextTransaction? _transaction;

        private int SaveChangesCoreSync()
        {
            try
            {
                return context.SaveChanges();
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

            _transaction = context.Database.BeginTransaction();
        }

        public async Task BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                return;

            _transaction = await context.Database
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
            // Do not dispose context — it is scoped and owned by DI container
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesCoreAsync(cancellationToken);
        }

        private async Task<int> SaveChangesCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await context.SaveChangesAsync(cancellationToken);
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
