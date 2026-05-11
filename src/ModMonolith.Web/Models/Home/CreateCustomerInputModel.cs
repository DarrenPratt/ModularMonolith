using System.ComponentModel.DataAnnotations;

namespace ModMonolith.Web.Models.Home;

public sealed class CreateCustomerInputModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;
}
