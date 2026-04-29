using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class ConfirmOrderDto
{
    [Required]
    [MinLength(10, ErrorMessage = "Shipping address must be at least 10 characters long")]
    public string ShippingAddress { get; set; } = string.Empty;
}
