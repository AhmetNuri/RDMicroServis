using OrderService.Domain;

namespace OrderService.Application.DTOs;

public class OrderSearchRequest
{
    public string? Query { get; set; }
    public OrderStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
