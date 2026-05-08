using AuditService.Application.DTOs;
using AuditService.Application.Services;
using Confluent.Kafka;

namespace AuditService.Infrastructure;

public class KafkaAuditConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaAuditConsumer> _logger;

    private static readonly string[] Topics =
    [
        "order.created", "order.cancelled",
        "payment.completed", "payment.failed",
        "inventory.reserved", "inventory.released",
        "product.created", "product.updated"
    ];

    public KafkaAuditConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<KafkaAuditConsumer> logger)
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
            GroupId = _configuration["Kafka:GroupId"] ?? "audit-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(Topics);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(1));
                        if (result == null) continue;

                        var dto = new AuditEventDto
                        {
                            EventType = result.Topic,
                            ServiceName = DetermineService(result.Topic),
                            Action = result.Topic.Split('.').LastOrDefault() ?? result.Topic,
                            ResourceType = result.Topic.Split('.').FirstOrDefault() ?? "unknown",
                            ResourceId = result.Message.Key ?? string.Empty,
                            Timestamp = DateTime.UtcNow,
                            NewValue = result.Message.Value
                        };

                        using var scope = _scopeFactory.CreateScope();
                        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                        await auditService.RecordEventAsync(dto);

                        consumer.Commit(result);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }, stoppingToken);
    }

    private static string DetermineService(string topic) => topic.Split('.')[0] switch
    {
        "order" => "order-service",
        "payment" => "payment-service",
        "inventory" => "inventory-service",
        "product" => "product-service",
        _ => "unknown"
    };
}
