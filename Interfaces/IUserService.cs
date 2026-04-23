using Models.Dtos;
namespace Interfaces;

public interface IUserService
{
    // รับ RegisterUserDto มาแล้วส่ง UserResponseDto กลับ
    Task<UserResponseDto> RegisterAsync(RegisterUserDto dto);
    // รับ LoginDto มาแล้วส่ง LoginResponseDto กลับ
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
}