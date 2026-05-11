using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModMonolith.Modules.Orders.Domain;

namespace ModMonolith.Modules.Orders.Infrastructure;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Number)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(order => order.CreatedUtc)
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId);
    }
}
