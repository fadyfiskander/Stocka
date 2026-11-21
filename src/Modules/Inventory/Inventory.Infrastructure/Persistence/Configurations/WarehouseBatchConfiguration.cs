
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    internal class WarehouseBatchConfiguration : IEntityTypeConfiguration<WarehouseBatch>
    {
        public void Configure(EntityTypeBuilder<WarehouseBatch> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).HasConversion(
                    id => id.Value,
                    value => new WarehouseBatchId(value))
                .ValueGeneratedNever();

            builder.Property(e => e.WarehouseId).HasConversion(
                    id => id.Value,
                    value => new WarehouseId(value))
                .IsRequired();

            builder.HasIndex(wb => new {
                   wb.BatchId, 
                   wb.WarehouseId})
                .IsUnique();

            builder.Property(e => e.BatchId).HasConversion(
                    id => id.Value,
                    value => new BatchId(value))
                .IsRequired();

            builder.Property(e => e.OrganizationId)
                .IsRequired();

            builder.Property(e => e.Quantity)
                .IsRequired();

            builder.Property(e => e.RowVersion)
                .IsRowVersion();

            // Unique constraint on WarehouseId + BatchId
            builder.HasIndex(e => new { e.WarehouseId, e.BatchId }, "ix_warehouse_batches_warehouse_id_batch_id")
                .IsUnique();

            // Domain invariant persisted at the database level: Quantity >= 0.
            builder.ToTable("WarehouseBatches", t =>
                t.HasCheckConstraint("CK_WarehouseBatches_Quantity_Nonnegative", "[Quantity] >= 0"));

            builder.HasOne<Batch>().WithMany()
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WarehouseBatches_Batches_BatchId");

            builder.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseBatches)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WarehouseBatches_Warehouses_WarehouseId");
        }
    }
}
