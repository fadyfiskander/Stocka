using Identity.Application.Repositories.Commands;
using Identity.Domain.Parties;
using Identity.Domain.Suppliers;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories.Commands.Bases;

namespace Identity.Infrastructure.Repositories.Commands
{
    internal class SupplierRepository : Repository<Supplier, PartyId>, ISupplierRepository
    {
        public SupplierRepository(AppIdentityDbContext context) : base(context)
        {
        }
    }
}
