using System.Data;

namespace Organization.Application.Common.Interfaces;

public interface IOrganizationDbConnection
{
    IDbConnection Connection { get; }
}
