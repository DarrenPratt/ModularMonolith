using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModMonolith.Modules.Orders.Domain;

namespace ModMonolith.Modules.Orders.Infrastructure;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines", "orders");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(line => line.Quantity)
            .IsRequired();

        builder.Property(line => line.UnitPrice)
            .HasPrecision(18, 2);
    }
}
