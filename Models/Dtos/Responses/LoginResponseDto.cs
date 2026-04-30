namespace Models.Dtos.Responses;

public class LoginResponseDto
{
    // retutn JWT Token
    public string Token {get; set;} = string.Empty;
    // return User info (Email, FirstName, LastName)
    public UserResponseDto User {get; set;} = null!;
}