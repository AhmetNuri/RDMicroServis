using AnalyticsService.Application.DTOs;
using AnalyticsService.Domain;
using AnalyticsService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(AnalyticsDbContext dbContext, ILogger<AnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordMetricAsync(RecordMetricRequest request)
    {
        var metric = new Metric
        {
            MetricName = request.MetricName,
            MetricValue = request.MetricValue,
            Dimensions = request.Dimensions,
            ServiceName = request.ServiceName,
            RecordedAt = DateTime.UtcNow
        };
        _dbContext.Metrics.Add(metric);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Metric recorded: {MetricName} = {Value}", request.MetricName, request.MetricValue);
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalOrders = await _dbContext.Metrics
            .Where(m => m.MetricName == "order.created" && m.RecordedAt >= thirtyDaysAgo)
            .CountAsync();

        var totalRevenue = await _dbContext.Metrics
            .Where(m => m.MetricName == "payment.completed" && m.RecordedAt >= thirtyDaysAgo)
            .SumAsync(m => m.MetricValue);

        var activeUsers = await _dbContext.Metrics
            .Where(m => m.MetricName == "order.created" && m.RecordedAt >= thirtyDaysAgo)
            .Select(m => m.Dimensions.ContainsKey("userId") ? m.Dimensions["userId"] : null)
            .Where(id => id != null)
            .Distinct()
            .CountAsync();

        var revenueByDay = await GetRevenueAsync(thirtyDaysAgo, now);
        var topProducts = await GetTopProductsAsync();
        var ordersByStatus = await GetOrdersByStatusAsync(thirtyDaysAgo, now);

        return new DashboardDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            ActiveUsers = activeUsers,
            TopProducts = topProducts.ToList(),
            OrdersByStatus = ordersByStatus,
            RevenueByDay = revenueByDay.ToList()
        };
    }

    public async Task<IList<RevenueDayDto>> GetRevenueAsync(DateTime from, DateTime to)
    {
        var metrics = await _dbContext.Metrics
            .Where(m => m.MetricName == "payment.completed" && m.RecordedAt >= from && m.RecordedAt <= to)
            .ToListAsync();

        return metrics
            .GroupBy(m => DateOnly.FromDateTime(m.RecordedAt))
            .Select(g => new RevenueDayDto
            {
                Date = g.Key,
                Revenue = g.Sum(m => m.MetricValue),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    public async Task<IList<ProductMetricDto>> GetTopProductsAsync(int count = 10)
    {
        var metrics = await _dbContext.Metrics
            .Where(m => m.MetricName == "order.item" && m.Dimensions.ContainsKey("productId"))
            .ToListAsync();

        return metrics
            .GroupBy(m => m.Dimensions.GetValueOrDefault("productId", ""))
            .Select(g => new ProductMetricDto
            {
                ProductId = g.Key,
                ProductName = g.FirstOrDefault()?.Dimensions.GetValueOrDefault("productName", g.Key) ?? g.Key,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(m => m.MetricValue)
            })
            .OrderByDescending(p => p.OrderCount)
            .Take(count)
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetOrdersByStatusAsync(DateTime from, DateTime to)
    {
        var metrics = await _dbContext.Metrics
            .Where(m => m.MetricName.StartsWith("order.") && m.RecordedAt >= from && m.RecordedAt <= to)
            .ToListAsync();

        return metrics
            .GroupBy(m => m.MetricName.Split('.').LastOrDefault() ?? m.MetricName)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
