using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalog.Infrastructure.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder
                .HasKey(C => C.Id);

            builder
                .Property(c => c.Id)
                .HasConversion(c => c.Value, c => CategoryId.FromGuid(c));

            builder
                .Property(c => c.Name)
                .HasColumnType("NVARCHAR(20)");

        }
    }
}
