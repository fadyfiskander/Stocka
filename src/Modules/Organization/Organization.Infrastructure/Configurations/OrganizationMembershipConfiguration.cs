using BuildingBlocks.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organization.Domain.OrganizationMemberships;
using Organization.Domain.Organizations;

namespace Organization.Infrastructure.Configurations
{
    internal class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
    {
        public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
        {
            // Matches the Dapper query table (singular name, under the Organization schema).
            builder.ToTable("OrganizationMembership");

            // Composite key: (OrganizationId, UserId) — a user is a member of an
            // organization at most once.
            builder
                .Property(m => m.OrganizationId)
                .HasConversion(id => id.Value, value => OrganizationId.From(value));


            builder
                .HasKey(m => new { m.OrganizationId, m.UserId });


            // Fast lookup of "which organizations does this user belong to".
            builder
                .HasIndex(m => m.UserId);

            builder
                .Property(m => m.Role)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);
        }
    }
}