using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateOrderAsync(request);
        return StatusCode(201, order);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        var (items, total) = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
        return Ok(new { data = items, total, page, pageSize });
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserOrders(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
        return Ok(new { data = items, total, page, pageSize });
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var order = await _orderService.UpdateStatusAsync(id, request.Status);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var success = await _orderService.CancelOrderAsync(id);
        return success ? Ok(new { message = "Order cancelled" }) : BadRequest(new { message = "Cannot cancel order" });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] OrderSearchRequest request)
    {
        var (items, total) = await _orderService.SearchOrdersAsync(request);
        return Ok(new { data = items, total, page = request.Page, pageSize = request.PageSize });
    }
}

public class UpdateStatusRequest
{
    public OrderStatus Status { get; set; }
}
