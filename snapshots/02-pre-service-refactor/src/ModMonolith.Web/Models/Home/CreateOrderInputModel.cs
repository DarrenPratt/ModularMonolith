using System.ComponentModel.DataAnnotations;

namespace ModMonolith.Web.Models.Home;

public sealed class CreateOrderInputModel
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}
