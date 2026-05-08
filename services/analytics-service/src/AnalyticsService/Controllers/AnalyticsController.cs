using AnalyticsService.Application.DTOs;
using AnalyticsService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin,Operator")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _analyticsService.GetDashboardAsync();
        return Ok(dashboard);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;
        var data = await _analyticsService.GetOrdersByStatusAsync(fromDate, toDate);
        return Ok(data);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;
        var data = await _analyticsService.GetRevenueAsync(fromDate, toDate);
        return Ok(data);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int count = 10)
    {
        var data = await _analyticsService.GetTopProductsAsync(count);
        return Ok(data);
    }

    [HttpPost("events")]
    public async Task<IActionResult> RecordEvent([FromBody] RecordMetricRequest request)
    {
        await _analyticsService.RecordMetricAsync(request);
        return Ok(new { message = "Metric recorded" });
    }
}
