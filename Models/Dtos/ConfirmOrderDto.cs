using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class ConfirmOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Shipping address is required")]
    public string ShippingAddress { get; set; } = string.Empty;
}
