using Identity.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations
{
    internal class PartyOrganizationConfiguration : IEntityTypeConfiguration<PartyOrganization>
    {
        public void Configure(EntityTypeBuilder<PartyOrganization> builder)
        {
            builder.ToTable("PartyOrganizations");

            builder.HasKey(x => new { x.PartyId, x.Organization });

            builder
                .Property(x => x.PartyId)
                .HasConversion(
                    id => id.Value,
                    value => PartyId.From(value))
                .IsRequired();

            builder
                .Property(x => x.Organization)
                .IsRequired();

            builder
                .HasOne(x => x.Party)
                .WithMany(p => p.PartyOrganizations)
                .HasForeignKey(x => x.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
