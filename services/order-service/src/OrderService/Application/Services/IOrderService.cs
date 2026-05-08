using OrderService.Application.DTOs;
using OrderService.Domain;

namespace OrderService.Application.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderDto?> GetOrderAsync(Guid id);
    Task<(IList<OrderDto> Items, int Total)> GetUserOrdersAsync(Guid userId, int page, int pageSize);
    Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status);
    Task<(IList<OrderDto> Items, int Total)> SearchOrdersAsync(OrderSearchRequest request);
    Task<bool> CancelOrderAsync(Guid id);
}
