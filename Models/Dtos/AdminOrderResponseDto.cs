namespace Models.Dtos;

public class AdminOrderResponseDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
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
