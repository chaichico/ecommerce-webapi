namespace Models.Dtos.Responses;

// User เข้าถึงไรได้บ้าง
public class UserResponseDto
{
    public string Email {get; set;} = string.Empty;
    public string FirstName {get; set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
}