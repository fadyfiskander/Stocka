using BuildingBlocks.Domain.Bases;
using BuildingBlocks.Domain.Enums;
using Organization.Domain.OrganizationMemberships;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Organization.Domain.Organizations
{
    public class Organization : AggregateRoot<OrganizationId>
    {
        public string Name { get; private set; } = default!;
        public string Description { get; private set; } = default!; 
        public string Slug { get; private set; } = default!;
        public string? LogoUrl { get; private set; } = default!;

        public List<OrganizationMembership> _organizationMemberships { get; private set; } = new();
        public IReadOnlyCollection<OrganizationMembership> OrganizationMemberships => _organizationMemberships.AsReadOnly();

        [SetsRequiredMembers]
        private Organization(string name, string description, string slug, string? logoUrl) 
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(description)) throw new ArgumentNullException(nameof(description));
            if (string.IsNullOrEmpty(slug)) throw new ArgumentNullException(nameof(slug));

            Id = OrganizationId.New();
            Name = name;
            Description = description;
            LogoUrl = logoUrl;
        }

        public static Organization Create(string name, string description, string slug, string? logoUrl)
        {
            return new Organization(name, description, slug, logoUrl);
        }

        public void Rename(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
        }

        public void UpdateDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) throw new ArgumentNullException(nameof(description));
            Description = description;
        }

        public void UpdateLogoUrl(string logoUrl)
        {
            if(string.IsNullOrEmpty(logoUrl)) 
                throw new ArgumentNullException(nameof(logoUrl));
            LogoUrl = logoUrl;
        }

        public void AddMember(Guid userId, OrganizationRole role)
        {
            var membership = OrganizationMembership.Create(this.Id, userId, role);
            if (membership == null) throw new ArgumentNullException(nameof(membership));
            _organizationMemberships.Add(membership);
        }

        public void RemoveAddMember(OrganizationMembership membership)
        {
            if (membership == null) throw new ArgumentNullException(nameof(membership));
            _organizationMemberships.Remove(membership);
        }
    }
}