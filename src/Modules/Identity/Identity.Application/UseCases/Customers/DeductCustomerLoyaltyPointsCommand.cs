using BuildingBlocks.Application.Abstractions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Repositories.Commands;
using Identity.Domain.Parties;
using MediatR;

namespace Identity.Application.UseCases.Customers;

internal sealed record DeductCustomerLoyaltyPointsCommand(
    Guid CustomerId,
    int Points) : ITransactionCommand;

internal sealed class DeductCustomerLoyaltyPointsCommandHandler
    : IRequestHandler<DeductCustomerLoyaltyPointsCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly ICurrentUserService _currentUserContext;

    public DeductCustomerLoyaltyPointsCommandHandler(
        ICustomerRepository customerRepository,
        IPartyRepository partyRepository,
        ICurrentUserService currentUserContext)
    {
        _customerRepository = customerRepository;
        _partyRepository = partyRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task Handle(
        DeductCustomerLoyaltyPointsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty.", nameof(request.CustomerId));

        if (request.Points <= 0)
            throw new ArgumentException("Points must be greater than zero.", nameof(request.Points));

        var organizationId = _currentUserContext.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

        var id = PartyId.From(request.CustomerId);
        var party = await _partyRepository.GetWithOrganizationsAsync(id);
        var customer = await _customerRepository.GetByIdAsync(id);

        if (party is null || customer is null || !party.PartyOrganizations.Any(
                membership => membership.Organization == organizationId.Value))
        {
            throw new KeyNotFoundException(
                $"Customer '{request.CustomerId}' was not found in the active organization.");
        }

        customer.DeductLoyaltyPoint(request.Points);
        await _customerRepository.UpdateAsync(customer);
    }
}
