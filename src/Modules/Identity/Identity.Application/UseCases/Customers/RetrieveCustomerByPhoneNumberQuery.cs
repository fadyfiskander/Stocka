using BuildingBlocks.Application.Abstractions;
using Identity.Application.Repositories.Queries;
using Identity.Contracts.DTOs;
using MediatR;

namespace Identity.Application.UseCases.Customers;

internal sealed record RetrieveCustomerByPhoneNumberQuery(string PhoneNumber)
    : IRequest<CustomerDto?>;

internal sealed class RetrieveCustomerByPhoneNumberQueryHandler
    : IRequestHandler<RetrieveCustomerByPhoneNumberQuery, CustomerDto?>
{
    private readonly ICustomerQueryRepository _customerQuery;
    private readonly ICurrentUserService _currentUserContext;

    public RetrieveCustomerByPhoneNumberQueryHandler(
        ICustomerQueryRepository customerQuery,
        ICurrentUserService currentUserContext)
    {
        _customerQuery = customerQuery;
        _currentUserContext = currentUserContext;
    }

    public async Task<CustomerDto?> Handle(
        RetrieveCustomerByPhoneNumberQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number cannot be empty.", nameof(request.PhoneNumber));

        var organizationId = _currentUserContext.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

        return await _customerQuery.GetByPhoneNumberAsync(
            organizationId.Value,
            request.PhoneNumber.Trim());
    }
}
