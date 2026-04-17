using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Models;
[Index(nameof(Email), IsUnique = true)]
public class User
{   
    // User
    public int Id {get; set;}
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email {get; set;} = string.Empty;

    [Required]
    public string FirstName {get; set;} = string.Empty;

    [Required]
    public string LastName {get; set;} = string.Empty;

    [Required]
    public string PasswordHash {get; set;} = string.Empty;

    public string? PhoneNumber {get; set;}

    // Order
    public List<Order> Orders {get; set;} = new();

}