using System.ComponentModel.DataAnnotations;

namespace AnalyticsService.Application.DTOs;

public class RecordMetricRequest
{
    [Required]
    public string MetricName { get; set; } = string.Empty;

    [Required]
    public decimal MetricValue { get; set; }

    public Dictionary<string, string> Dimensions { get; set; } = new();

    public string ServiceName { get; set; } = string.Empty;
}
