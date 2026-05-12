using System.ComponentModel.DataAnnotations;

namespace Models.Dtos.Requests;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}