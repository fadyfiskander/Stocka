using Identity.Domain.Employees;
using Identity.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations
{
    internal class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");

            // Shared primary key: PartyId is both the primary key and the foreign key to Parties.
            builder.HasKey(e => e.Id);

            builder
                .Property(e => e.Id)
                .HasConversion(id => id.Value, value => PartyId.From(value))
                .HasColumnName("PartyId")
                .ValueGeneratedNever();

            builder
                .Property(e => e.JobTitle)
                .HasMaxLength(100)
                .IsRequired();

            // PK and FK in the same column (PartyId). A Party can have at most one Employee row.
            // Deleting the Party cascades to its Employee.
            builder
                .HasOne(e => e.Party)
                .WithOne()
                .HasForeignKey<Employee>(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Employees_Parties_PartyId");
        }
    }
}
