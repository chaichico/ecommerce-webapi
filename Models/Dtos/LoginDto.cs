namespace Models.Dtos;
using System.ComponentModel.DataAnnotations;
public class LoginDto
{
    // รับแค่ email และ password
    [Required]
    [EmailAddress]
    public string Email {get; set;} = string.Empty;

    [Required]
    public string Password {get; set;} = string.Empty;
}