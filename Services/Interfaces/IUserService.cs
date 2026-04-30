using Models.Dtos.Requests;
using Models.Dtos.Responses;
namespace Services.Interfaces;

public interface IUserService
{
    // รับ RegisterUserDto มาแล้วส่ง UserResponseDto กลับ
    Task<UserResponseDto> RegisterAsync(RegisterUserDto dto);
    // รับ LoginDto มาแล้วส่ง LoginResponseDto กลับ
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
}