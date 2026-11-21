using Identity.Domain.Suppliers;
using Identity.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations
{
    internal class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");

            // Shared primary key: PartyId is both the primary key and the foreign key to Parties.
            builder.HasKey(s => s.Id);

            builder
                .Property(s => s.Id)
                .HasConversion(id => id.Value, value => PartyId.From(value))
                .HasColumnName("PartyId")
                .ValueGeneratedNever();

            builder
                .Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder
                .Property(s => s.Email)
                .HasMaxLength(256)
                .IsRequired(false);

            builder
                .Property(s => s.Address)
                .HasMaxLength(250)
                .IsRequired(false);

            // PK and FK in the same column (PartyId). A Party can have at most one Supplier row.
            // Deleting the Party cascades to its Supplier.
            builder
                .HasOne(s => s.Party)
                .WithOne()
                .HasForeignKey<Supplier>(s => s.Id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Suppliers_Parties_PartyId");
        }
    }
}
