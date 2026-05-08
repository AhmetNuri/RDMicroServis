using System.Text.Json;
using AnalyticsService.Application.DTOs;
using AnalyticsService.Application.Services;
using Confluent.Kafka;

namespace AnalyticsService.Infrastructure;

public class KafkaAnalyticsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaAnalyticsConsumer> _logger;

    public KafkaAnalyticsConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<KafkaAnalyticsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka__BootstrapServers"] ?? _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "analytics-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(["order.created", "payment.completed"]);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(1));
                        if (result == null) continue;

                        using var scope = _scopeFactory.CreateScope();
                        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                        var payload = JsonSerializer.Deserialize<JsonElement>(result.Message.Value);
                        var dimensions = new Dictionary<string, string>
                        {
                            ["topic"] = result.Topic,
                            ["key"] = result.Message.Key ?? string.Empty
                        };

                        if (payload.TryGetProperty("userId", out var userId))
                            dimensions["userId"] = userId.GetString() ?? string.Empty;
                        if (payload.TryGetProperty("totalAmount", out var amount))
                            dimensions["amount"] = amount.ToString();

                        decimal metricValue = 1m;
                        if (result.Topic == "payment.completed" && payload.TryGetProperty("totalAmount", out var paymentAmount))
                            metricValue = paymentAmount.TryGetDecimal(out var d) ? d : 1m;

                        await analyticsService.RecordMetricAsync(new RecordMetricRequest
                        {
                            MetricName = result.Topic,
                            MetricValue = metricValue,
                            Dimensions = dimensions,
                            ServiceName = "analytics-service"
                        });

                        consumer.Commit(result);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing analytics message");
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }, stoppingToken);
    }
}
