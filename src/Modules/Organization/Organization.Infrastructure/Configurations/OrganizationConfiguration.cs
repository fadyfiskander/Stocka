
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organization.Domain.OrganizationMemberships;
using Organization.Domain.Organizations;
using OrganizationAggregate = Organization.Domain.Organizations.Organization;

namespace Organization.Infrastructure.Configurationss
{
    internal class OrganizationConfiguration
        : IEntityTypeConfiguration<OrganizationAggregate>
    {
        public void Configure(EntityTypeBuilder<OrganizationAggregate> builder)
        {
            builder.ToTable("Organization");

            builder.HasKey(o => o.Id);
            builder
                .Property(o => o.Id)
                .HasConversion(id => id.Value, value => OrganizationId.From(value));


            builder.Property(o => o.Name)
                .HasColumnType("NVARCHAR(100)")
                .IsRequired();

            builder.Property(o => o.Description)
                .HasColumnType("NVARCHAR(600)")
                .IsRequired();

            builder.Property(o => o.Slug)
                .HasColumnType("NVARCHAR(100)")
                .IsRequired();

            builder.HasIndex(o => o.Slug)
                .IsUnique();

            builder.Property(o => o.LogoUrl)
                .HasColumnType("NVARCHAR(500)")
                .IsRequired(false);

            // Private backing field navigation
            builder.HasMany<OrganizationMembership>(
                    "_organizationMemberships")
                .WithOne()
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation("_organizationMemberships")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Read-only computed wrapper — EF discovers it by convention as a
            // second navigation and creates a shadow FK (OrganizationId1).
            // Access always goes through the backing field above.
            builder.Ignore(o => o.OrganizationMemberships);
        }
    }
}
