using MediatR;
using Organization.Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Organization.Application.UseCases.Organizations
{
    internal class OrganizationAccessQuery : IRequest<IEnumerable<OrganizationAccessDto>>
    {
        public Guid UserId { get; set; }
    }

    public class OrganizationAccessQueryHandler : IRequestHandler<OrganizationAccessQuery, IEnumerable<OrganizationAccessDto>>
    {
         
        Task<IEnumerable<OrganizationAccessDto>> IRequestHandler<OrganizationAccessQuery, IEnumerable<OrganizationAccessDto>>.Handle(OrganizationAccessQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
