namespace Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    // Order
    public int Id {get; set;}

    [Required]
    public string OrderNumber {get; set;} = string.Empty;

    public DateTime OrderDate {get; set;} = DateTime.UtcNow;

    [Required]
    public string Status {get; set;} = OrderStatus.Pending;

    [Required]
    public string ShippingAddress {get; set;} = string.Empty;

    // User
    [Required]
    public int UserId {get; set;}
    public User User {get; set;} = null!;       // must have User

    // OrderItem
    public List<OrderItem> Items {get; set;} = new();

    // Pricing
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice {get; set;}

}