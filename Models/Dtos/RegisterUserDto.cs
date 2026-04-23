namespace Models.Dtos;

using System.ComponentModel.DataAnnotations;

// User Register 
public class RegisterUserDto
{
    [Required]
    [EmailAddress]
    public string Email {get; set;} = string.Empty;

    [Required]
    public string FirstName {get; set;} = string.Empty;
    [Required]
    public string LastName {get; set;} = string.Empty;
    public string? PhoneNumber {get; set;}

    [Required]
    [MinLength(6)]
    public string Password {get; set;} = string.Empty;

    [Required]
    [Compare("Password", ErrorMessage = "Password do not match")]
    public string ConfirmPassword {get; set;} = string.Empty;
}