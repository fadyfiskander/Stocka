using BuildingBlocks.Application.Abstractions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Repositories.Commands;
using Identity.Contracts.DTOs;
using Identity.Domain.Parties;
using MediatR;

namespace Identity.Application.UseCases.Customers;

internal sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string PhoneNumber) : ITransactionCommand<CustomerDto>;

internal sealed class UpdateCustomerCommandHandler
    : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly ICurrentUserService _currentUserContext;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IPartyRepository partyRepository,
        ICurrentUserService currentUserContext)
    {
        _customerRepository = customerRepository;
        _partyRepository = partyRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<CustomerDto> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var organizationId = _currentUserContext.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

        var customerId = PartyId.From(request.CustomerId);
        var party = await _partyRepository.GetWithOrganizationsAsync(customerId);

        if (party is null || !party.PartyOrganizations.Any(
                membership => membership.Organization == organizationId.Value))
        {
            throw new KeyNotFoundException(
                $"Customer '{request.CustomerId}' was not found in the active organization.");
        }

        var customer = await _customerRepository.GetByIdAsync(customerId);

        if (customer is null)
            throw new KeyNotFoundException($"Customer '{request.CustomerId}' was not found.");

        party.UpdateName(request.FirstName, request.LastName);
        party.UpdatePhoneNumber(request.PhoneNumber);

        await _partyRepository.UpdateAsync(party);

        return new CustomerDto(
            customer.Id.Value,
            party.FirstName,
            party.LastName,
            party.PhoneNumber,
            customer.LoyaltyPoint);
    }

    private static void Validate(UpdateCustomerCommand request)
    {
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty.", nameof(request.CustomerId));

        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ArgumentException("First name cannot be empty.", nameof(request.FirstName));

        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(request.LastName));

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number cannot be empty.", nameof(request.PhoneNumber));
    }
}
