using Organization.Application.Common.Interfaces;
using System.Data;

namespace Organization.Infrastructure.Repositories.Queries.Bases
{
    /// <summary>
    /// Read-side (CQRS) query repository base over raw SQL (Dapper).
    /// <para>
    /// Each query repository receives a short-lived <see cref="IDbConnection"/>
    /// (registered per-request in <c>OrganizationModuleRegistration</c>).
    /// Dapper opens a closed connection on execution and closes it when it opened it —
    /// so implementations never need to manage connection lifecycle per call.
    /// </para>
    /// </summary>
    internal abstract class QueryRepository
    {
        /// <summary>Dapper-target connection. Named for readability at call sites.</summary>
        protected readonly IDbConnection _connection;

        protected const string TableOrganizations = "Organization.Organization";
        protected const string TableOrganizationMemberships = "Organization.OrganizationMembership";

        protected QueryRepository(IOrganizationDbConnection connection)
        {
            _connection = connection.Connection;
        }

        /// <summary>
        /// Escapes SQL LIKE wildcards from caller input (<c>% _ [ \</c>), keeping the
        /// contains-search safe. Callers wrap the returned value in <c>%…%</c>.
        /// </summary>
        protected static string EscapeLike(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
    }
}
