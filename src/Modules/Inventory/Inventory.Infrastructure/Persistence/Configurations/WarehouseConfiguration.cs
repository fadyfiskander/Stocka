
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    internal class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder
                .HasKey(w => w.Id);

            builder
                .Property(w => w.Id)
                .HasConversion(w => w.Value, w => WarehouseId.From(w))
                .ValueGeneratedNever();


            builder
                .Property(w => w.Name)
                .HasColumnType("NVARCHAR(50)")
                .IsRequired();
            builder
                .HasIndex(w => new { w.OrganizationId, w.Name })
                .IsUnique();

            builder
                .Property(w => w.Location)
                .HasColumnType("NVARCHAR(100)")
                .IsRequired();

            builder
                .Property(w => w.Description)
                .HasColumnType("NVARCHAR(600)")
                .IsRequired(false);

            // Enums are stored as int (consistent with Dapper DTOs); avoid HasConversion<string>.
            builder.Property(e => e.Type)
                .IsRequired();

            // The physical table is singular ("Warehouse") — matches InitialCreate migration and Dapper queries.
            builder.ToTable("Warehouse");

            builder
                .Property(w => w.OrganizationId)
                .IsRequired();

            builder
                .HasMany(w => w.WarehouseBatches)
                .WithOne(w => w.Warehouse)
                .HasForeignKey(w => w.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
