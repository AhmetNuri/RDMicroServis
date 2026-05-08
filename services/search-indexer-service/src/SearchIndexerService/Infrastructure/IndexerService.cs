using System.Text.Json;
using Confluent.Kafka;
using SearchIndexerService.Application.Services;
using SearchIndexerService.Domain;

namespace SearchIndexerService.Infrastructure;

public class IndexerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexerService> _logger;

    public IndexerService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<IndexerService> logger)
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
            GroupId = _configuration["Kafka:GroupId"] ?? "search-indexer-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(["product.created", "product.updated", "product.deleted"]);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(1));
                        if (result == null) continue;

                        using var scope = _scopeFactory.CreateScope();
                        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

                        if (result.Topic == "product.deleted")
                        {
                            await searchService.DeleteProductAsync(result.Message.Key ?? result.Message.Value);
                            _logger.LogInformation("Deleted product {Key} from index", result.Message.Key);
                        }
                        else
                        {
                            var doc = JsonSerializer.Deserialize<ProductSearchDocument>(result.Message.Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (doc != null)
                            {
                                await searchService.IndexProductAsync(doc);
                                _logger.LogInformation("Indexed product {Id} from topic {Topic}", doc.Id, result.Topic);
                            }
                        }

                        consumer.Commit(result);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing indexer message");
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
