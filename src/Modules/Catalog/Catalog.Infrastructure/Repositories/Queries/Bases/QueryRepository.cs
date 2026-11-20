using Catalog.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Catalog.Application.Repositories.Queries.Bases
{
    public abstract class QueryRepository
    {
        protected readonly IDbConnection _connection;
        private const string Schema = "Catalog";
        protected const string TableProducts = $"{Schema}.Products";
        protected const string TableCategories = $"{Schema}.Categories";

        protected QueryRepository(ICatalogDbConnection connection)
        {
            _connection = connection.Connection;
        }

        protected IDbConnection GetConnection() => _connection;
    }

}
