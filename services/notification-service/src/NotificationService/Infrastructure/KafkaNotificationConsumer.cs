using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Application.DTOs;
using NotificationService.Application.Services;
using NotificationService.Hubs;

namespace NotificationService.Infrastructure;

public class KafkaNotificationConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<KafkaNotificationConsumer> _logger;

    public KafkaNotificationConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHubContext<NotificationHub> hubContext,
        ILogger<KafkaNotificationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka__BootstrapServers"] ?? _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "notification-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("notification.send");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(1));
                        if (result == null) continue;

                        var request = JsonSerializer.Deserialize<SendNotificationRequest>(result.Message.Value,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (request != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                            var notification = await notificationService.CreateNotificationAsync(request);

                            await _hubContext.Clients.Group(request.UserId.ToString())
                                .SendAsync("ReceiveNotification", notification, stoppingToken);

                            _logger.LogInformation("Processed notification for user {UserId}", request.UserId);
                        }

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
}
