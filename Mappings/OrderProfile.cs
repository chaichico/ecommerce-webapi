using AutoMapper;
using Models.Dtos.Responses;
using Models.Entities;

namespace Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // OrderItem → OrderItemResponseDto
        // SubTotal เป็น computed property อยู่แล้ว AutoMapper อ่านได้เลย
        CreateMap<OrderItem, OrderItemResponseDto>();

        // Order → OrderResponseDto
        // Items เป็น collection ให้ AutoMapper map nested ให้อัตโนมัติ
        CreateMap<Order, OrderResponseDto>();

        // User → AdminUserInfoDto
        CreateMap<User, AdminUserInfoDto>();

        // Order → AdminOrderResponseDto
        // field User และ Items เป็น nested object ที่ map ต่อไปได้
        CreateMap<Order, AdminOrderResponseDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
    }
}