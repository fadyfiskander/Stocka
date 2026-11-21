using BuildingBlocks.Application.Abstractions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Repositories.Commands;
using Identity.Contracts.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.UseCases.Customers
{
    internal class RegisterCustomerCommand : ITransactionCommand<CustomerDto>
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
    }

    internal class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, CustomerDto>
    {
        private readonly ICustomerRepository _customerCommand;
        private readonly IPartyRepository _partyCommand;
        private readonly ICurrentUserService _currentUserContext;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public RegisterCustomerCommandHandler(ICustomerRepository customerCommand, ICurrentUserService currentUserContext, IPartyRepository partyCommand, IIdentityUnitOfWork unitOfWork)
        {
            _customerCommand = customerCommand;
            _currentUserContext = currentUserContext;
            _partyCommand = partyCommand;
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerDto> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                throw new ArgumentException("First Name cannot be empty.", nameof(request.FirstName));
            if (string.IsNullOrWhiteSpace(request.LastName))
                throw new ArgumentException("Last Name cannot be empty.", nameof(request.LastName));
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new ArgumentException("PhoneNumber cannot be empty.", nameof(request.PhoneNumber));

            var organizationId = _currentUserContext.OrganizationId;
            if (organizationId is null)
                throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

            var party = Domain.Parties.Party.Create(organizationId.Value, request.FirstName, request.LastName, request.PhoneNumber);
            var customer = Domain.Customers.Customer.Create(party.Id);

            await _partyCommand.AddAsync(party);
            await _customerCommand.AddAsync(customer);

            return new CustomerDto
            (
                customer.Id.Value,
                party.FirstName,
                party.LastName,
                party.PhoneNumber,
                0
            );
        }
    }
}