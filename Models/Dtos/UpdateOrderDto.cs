namespace Models.Dtos;

using System.ComponentModel.DataAnnotations;

public class UpdateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    // reuse เลยเพราะ items ใหม่จะ replace แทน
    public List<CreateOrderItemDto> Items {get; set;} = new();
}
