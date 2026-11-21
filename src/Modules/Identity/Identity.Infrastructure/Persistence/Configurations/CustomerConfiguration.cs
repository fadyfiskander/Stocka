using Identity.Domain.Customers;
using Identity.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations
{
    internal class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // Domain invariant persisted at the database level: LoyaltyPoint >= 0.
            builder.ToTable("Customers", t =>
                t.HasCheckConstraint("CK_Customers_LoyaltyPoint_Nonnegative", "[LoyaltyPoint] >= 0"));

            // Shared primary key: PartyId is both the primary key and the foreign key to Parties.
            builder.HasKey(c => c.Id);

            builder
                .Property(c => c.Id)
                .HasConversion(id => id.Value, value => PartyId.From(value))
                .HasColumnName("PartyId")
                .ValueGeneratedNever();

            builder
                .Property(c => c.LoyaltyPoint)
                .IsRequired();

            // PK and FK in the same column (PartyId). A Party can have at most one Customer row.
            // Deleting the Party cascades to its Customer.
            builder
                .HasOne(c => c.Party)
                .WithOne()
                .HasForeignKey<Customer>(c => c.Id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Customers_Parties_PartyId");
        }
    }
}
