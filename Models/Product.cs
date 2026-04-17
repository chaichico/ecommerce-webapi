namespace Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    // Product
    public int Id {get; set;}

    [Required]
    public string ProductName {get; set;} = string.Empty;

    [Required]
    public string Description {get; set;} = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price {get; set;}

    public int Stock {get; set;}
    public bool IsActive {get; set;} = true;
    
}