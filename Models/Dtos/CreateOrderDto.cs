using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}