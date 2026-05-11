using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModMonolith.Modules.Customers.Domain;

namespace ModMonolith.Modules.Customers.Infrastructure;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(customer => customer.CreatedUtc)
            .IsRequired();

        builder.HasIndex(customer => customer.Email)
            .IsUnique();
    }
}
