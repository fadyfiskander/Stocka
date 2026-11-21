using BuildingBlocks.Application.Common.Interfaces;
using Identity.Domain.Customers;
using Identity.Domain.Parties;

namespace Identity.Application.Repositories.Commands
{
    /// <summary>
    /// Write-side repository for the Customer role.
    /// The key is <c>PartyId</c> — it is both the primary key and the foreign key to Parties (shared PK).
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer, PartyId>
    {
    }
}
