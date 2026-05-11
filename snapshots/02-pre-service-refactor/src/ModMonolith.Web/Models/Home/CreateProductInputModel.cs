using System.ComponentModel.DataAnnotations;

namespace ModMonolith.Web.Models.Home;

public sealed class CreateProductInputModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}
