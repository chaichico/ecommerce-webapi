namespace Models.Dtos;
using System.Text.Json.Serialization;
using Models.Enums;

public class AdminOrderResponseDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public AdminUserInfoDto User { get; set; } = null!;
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class AdminUserInfoDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
