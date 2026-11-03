using Microsoft.Data.SqlClient;
using Organization.Application.Common.Interfaces;
using System.Data;

namespace Organization.Infrastructure;

internal sealed class OrganizationDbConnection : IOrganizationDbConnection, IDisposable
{
    public OrganizationDbConnection(string connectionString)
    {
        Connection = new SqlConnection(connectionString);
    }

    public IDbConnection Connection { get; }

    public void Dispose()
    {
        Connection.Dispose();
    }
}
