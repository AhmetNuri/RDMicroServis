namespace AnalyticsService.Domain;

public class Metric
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MetricName { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public Dictionary<string, string> Dimensions { get; set; } = new();
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string ServiceName { get; set; } = string.Empty;
}
