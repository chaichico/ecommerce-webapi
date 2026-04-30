using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities;

public class OrderItem
{
    // OrderItem
    public int Id {get; set;}

    // Order
    [Required]
    public int OrderId {get; set;}

    public Order Order {get; set;} = null!;     // must have Order

    // Product
    [Required]
    public int ProductId {get; set;}

    [Required]
    public string ProductName {get; set;} = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity {get; set;}

    // Pricing
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice {get; set;}
    public decimal SubTotal => Quantity * UnitPrice;
}