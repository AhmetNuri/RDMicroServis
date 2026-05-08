using AnalyticsService.Application.DTOs;

namespace AnalyticsService.Application.Services;

public interface IAnalyticsService
{
    Task RecordMetricAsync(RecordMetricRequest request);
    Task<DashboardDto> GetDashboardAsync();
    Task<IList<RevenueDayDto>> GetRevenueAsync(DateTime from, DateTime to);
    Task<IList<ProductMetricDto>> GetTopProductsAsync(int count = 10);
    Task<Dictionary<string, int>> GetOrdersByStatusAsync(DateTime from, DateTime to);
}
