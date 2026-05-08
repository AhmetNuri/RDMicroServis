using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure;

public class RabbitMqEmailConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEmailConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName = "send-email";
    private const string DlqName = "send-email.dlq";
    private const string DeadLetterExchange = "send-email.dlx";
    private const int MaxRetries = 3;

    public RabbitMqEmailConsumer(IConfiguration configuration, ILogger<RabbitMqEmailConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                await InitializeRabbitMqAsync();

                var consumer = new AsyncEventingBasicConsumer(_channel!);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var retryCount = GetRetryCount(ea.BasicProperties);

                    try
                    {
                        var emailPayload = JsonSerializer.Deserialize<EmailPayload>(message,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        _logger.LogInformation("Simulating email send to {To}: {Subject}",
                            emailPayload?.To, emailPayload?.Subject);

                        await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process email message, retry count: {RetryCount}", retryCount);
                        if (retryCount >= MaxRetries)
                        {
                            _logger.LogWarning("Max retries reached, sending to DLQ");
                            await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                        }
                        else
                        {
                            await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
                        }
                    }
                };

                await _channel!.BasicConsumeAsync(QueueName, false, consumer, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "RabbitMQ consumer error");
            }
        }, stoppingToken);
    }

    private async Task InitializeRabbitMqAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ__Host"] ?? _configuration["RabbitMQ:Host"] ?? "rabbitmq",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ__Username"] ?? _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ__Password"] ?? _configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Declare DLX
        await _channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true);
        await _channel.QueueDeclareAsync(DlqName, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(DlqName, DeadLetterExchange, QueueName);

        // Declare main queue with DLX
        var args = new Dictionary<string, object?> { { "x-dead-letter-exchange", DeadLetterExchange } };
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        await _channel.BasicQosAsync(0, 10, false);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties props)
    {
        if (props.Headers != null && props.Headers.TryGetValue("x-retry-count", out var count))
            return Convert.ToInt32(count);
        return 0;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

public class EmailPayload
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
