using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class ApproveOrdersDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one order id is required")]
    public List<int> OrderIds { get; set; } = new();
}
