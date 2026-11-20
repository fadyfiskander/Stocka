
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    internal class BatchConfiguration : IEntityTypeConfiguration<Batch>
    {
        public void Configure(EntityTypeBuilder<Batch> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(b => b.Id)
                .HasConversion(b => b.Value, b => BatchId.From(b))
                .ValueGeneratedNever()
                .IsRequired();

            builder
                .Property(b => b.PurchaseItemId)
                .IsRequired();

            builder
                .Property(b => b.ProductId)
                .IsRequired();

            builder
                .Property(b => b.OrganizationId)
                .IsRequired();

            builder
                .Property(b => b.InitialQuantity)
                .IsRequired();

            builder.Property(b => b.RemainingQuantity)
                .IsRequired();

            builder
                .Property(b => b.UnitCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder
                .Property(b => b.RowVersion)
                .IsRowVersion();

            // Frequent report filters: product-level and purchase-level batch lookups, organization-scoped.
            builder.HasIndex(b => new { b.OrganizationId, b.ProductId });
            builder.HasIndex(b => new { b.OrganizationId, b.PurchaseItemId });

            // Domain invariant persisted at the database level: RemainingQuantity >= 0.
            builder.ToTable("Batches", t =>
                t.HasCheckConstraint("CK_Batches_RemainingQuantity_Nonnegative", "[RemainingQuantity] >= 0"));
        }
    }
}
