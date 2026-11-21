using BuildingBlocks.Application.Common.Interfaces;
using Identity.Domain.Parties;
using Identity.Domain.Suppliers;

namespace Identity.Application.Repositories.Commands
{
    /// <summary>
    /// Write-side repository for the Supplier (company) role.
    /// The key is <c>PartyId</c> — it is both the primary key and the foreign key to Parties (shared PK).
    /// </summary>
    public interface ISupplierRepository : IRepository<Supplier, PartyId>
    {
    }
}
