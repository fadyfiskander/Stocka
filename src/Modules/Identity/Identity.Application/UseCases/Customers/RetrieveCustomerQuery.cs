using Identity.Application.Repositories.Queries;
using Identity.Contracts.DTOs;
using Identity.Domain.Parties;
using MediatR;

namespace Identity.Application.UseCases.Customers
{
    internal class RetrieveCustomerQuery : IRequest<CustomerDto?>
    {
        public Guid CustomerId { get; set; }
    }

    internal class RetrieveCustomerQueryHandler : IRequestHandler<RetrieveCustomerQuery, CustomerDto?>
    {
        private readonly ICustomerQueryRepository _customerQuery;

        public RetrieveCustomerQueryHandler(ICustomerQueryRepository customerQuery)
        {
            _customerQuery = customerQuery;
        }

        public async Task<CustomerDto?> Handle(RetrieveCustomerQuery request, CancellationToken cancellationToken)
        {
            if (request.CustomerId == Guid.Empty)
                throw new ArgumentException("CustomerId cannot be empty.", nameof(request.CustomerId));

            return await _customerQuery.GetByIdAsync(PartyId.From(request.CustomerId));
        }
    }
}