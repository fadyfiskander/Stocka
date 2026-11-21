using Identity.Application.Repositories.Commands;
using Identity.Domain.Employees;
using Identity.Domain.Parties;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories.Commands.Bases;

namespace Identity.Infrastructure.Repositories.Commands
{
    internal class EmployeeRepository : Repository<Employee, PartyId>, IEmployeeRepository
    {
        public EmployeeRepository(AppIdentityDbContext context) : base(context)
        {
        }
    }
}
