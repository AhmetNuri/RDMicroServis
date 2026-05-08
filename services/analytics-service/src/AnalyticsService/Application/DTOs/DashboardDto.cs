namespace AnalyticsService.Application.DTOs;

public class DashboardDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveUsers { get; set; }
    public List<ProductMetricDto> TopProducts { get; set; } = [];
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public List<RevenueDayDto> RevenueByDay { get; set; } = [];
}

public class ProductMetricDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class RevenueDayDto
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
