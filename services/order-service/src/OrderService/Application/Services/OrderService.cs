using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.DTOs;
using OrderService.Domain;
using OrderService.Infrastructure;

namespace OrderService.Application.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderDbContext dbContext, ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
    {
        var orderNumber = GenerateOrderNumber();
        var order = new Order
        {
            UserId = request.UserId,
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            Notes = request.Notes,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.Quantity * i.UnitPrice
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

        var outboxMessage = new OutboxMessage
        {
            OrderId = order.Id,
            EventType = "order.created",
            Payload = JsonSerializer.Serialize(MapToDto(order))
        };
        order.OutboxMessages.Add(outboxMessage);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        _logger.LogInformation("Order created: {OrderNumber}", orderNumber);
        return MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderAsync(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<(IList<OrderDto> Items, int Total)> GetUserOrdersAsync(Guid userId, int page, int pageSize)
    {
        var query = _dbContext.Orders.Include(o => o.Items).Where(o => o.UserId == userId).AsNoTracking();
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto).ToList(), total);
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return null;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        var outboxMessage = new OutboxMessage
        {
            OrderId = order.Id,
            EventType = $"order.{status.ToString().ToLower()}",
            Payload = JsonSerializer.Serialize(MapToDto(order))
        };
        _dbContext.OutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<(IList<OrderDto> Items, int Total)> SearchOrdersAsync(OrderSearchRequest request)
    {
        var query = _dbContext.Orders.Include(o => o.Items).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(q) ||
                (o.Notes != null && o.Notes.ToLower().Contains(q)) ||
                o.Items.Any(i => i.ProductName.ToLower().Contains(q)));
        }

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
        return (items.Select(MapToDto).ToList(), total);
    }

    public async Task<bool> CancelOrderAsync(Guid id)
    {
        var order = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return false;
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled) return false;

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        var outboxMessage = new OutboxMessage
        {
            OrderId = order.Id,
            EventType = "order.cancelled",
            Payload = JsonSerializer.Serialize(MapToDto(order))
        };
        _dbContext.OutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        Notes = order.Notes,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}
