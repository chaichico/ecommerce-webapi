namespace Models.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Models.Enums;

public class OrderResponseDto
{
    public int Id { get; set; }
    public string OrderNumber {get; set;} = string.Empty;
    public DateTime OrderDate {get; set;}
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status {get; set;}
    public decimal TotalPrice {get; set;}
    public List<OrderItemResponseDto> Items {get; set;} = new();
}

public class OrderItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName {get; set;} = string.Empty;
    public int Quantity {get; set;}
    public decimal UnitPrice {get; set;}
    public decimal SubTotal {get; set;}
}