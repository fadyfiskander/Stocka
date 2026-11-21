using Catalog.Domain.Categories;
using Catalog.Domain.Entities;
using Catalog.Domain.Primitives;
using Catalog.Domain.Primitives.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalog.Infrastructure.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);
            builder
                .Property(p => p.Id)
                .HasConversion(p => p.Value, p => ProductId.FromGuid(p));

            builder
                .Property(p => p.Name)
                .HasColumnType("NVARCHAR(100)")
                .IsRequired();

            builder
                .Property(p => p.Barcode)
                .HasMaxLength(20)
                .IsRequired(false);

            builder
                .Property(p => p.SellingPrice)
                .HasColumnType("DECIMAL(7,2)");

            builder
                .Property(p => p.ImagePath)
                .IsRequired(false);

            builder
                .Property(p => p.CategoryId)
                .HasConversion(p => p.Value, p => CategoryId.FromGuid(p));

            builder
                .HasOne<Category>()
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}