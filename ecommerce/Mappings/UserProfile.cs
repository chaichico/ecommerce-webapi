using AutoMapper;
using Models.Dtos.Responses;
using Models.Entities;

namespace Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // User → UserResponseDto (3 fields ตรงกันหมด)
        CreateMap<User, UserResponseDto>();
    }
}