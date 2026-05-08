using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure;

namespace OrderService.Infrastructure;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka__BootstrapServers"] ?? _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            Acks = Acks.All,
            EnableIdempotence = true
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await producer.ProduceAsync(message.EventType, new Message<string, string>
                        {
                            Key = message.OrderId.ToString(),
                            Value = message.Payload
                        }, stoppingToken);

                        message.ProcessedAt = DateTime.UtcNow;
                        _logger.LogInformation("Published outbox message {Id} to topic {Topic}", message.Id, message.EventType);
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        _logger.LogWarning(ex, "Failed to publish outbox message {Id}, retry {Retry}", message.Id, message.RetryCount);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "OutboxPublisher error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
