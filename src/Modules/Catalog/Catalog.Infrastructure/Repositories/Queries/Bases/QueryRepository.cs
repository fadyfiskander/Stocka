using BuildingBlocks.Application.Abstractions;
using Catalog.Application.Common.Interfaces;
using System.Data;

namespace Catalog.Application.Repositories.Queries.Bases
{
    public abstract class QueryRepository
    {
        protected readonly IDbConnection _connection;
        protected readonly ICurrentUserService _currentUser;
        private const string Schema = "Catalog";
        protected const string TableProducts = $"{Schema}.Products";
        protected const string TableCategories = $"{Schema}.Categories";

        protected Guid OrganizationId
        {
            get
            {
                var organizationId = _currentUser.OrganizationId
                    ?? throw new InvalidOperationException(
                        $"Cannot resolve organization context from current user '{_currentUser.UserId}': {nameof(_currentUser.OrganizationId)} is null.");

                return organizationId;
            }
        }
        protected QueryRepository(ICatalogDbConnection connection, ICurrentUserService currentUser)
        {
            _connection = connection.Connection;
            _currentUser = currentUser;
        }

        protected IDbConnection GetConnection() => _connection;
    }

}
