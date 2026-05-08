using AuditService.Application.DTOs;
using AuditService.Domain;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace AuditService.Infrastructure;

public class ElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string IndexName = "audit-events";

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        var uri = configuration["Elasticsearch__Uri"] ?? configuration["Elasticsearch:Uri"] ?? "http://elasticsearch:9200";
        var settings = new ElasticsearchClientSettings(new Uri(uri))
            .DefaultIndex(IndexName);
        _client = new ElasticsearchClient(settings);
    }

    public async Task EnsureIndexAsync()
    {
        var existsResponse = await _client.Indices.ExistsAsync(IndexName);
        if (!existsResponse.Exists)
        {
            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Mappings(m => m
                    .Properties<AuditEvent>(p => p
                        .Keyword(k => k.EventId)
                        .Text(t => t.EventType)
                        .Text(t => t.Action)
                        .Text(t => t.ResourceType)
                        .Keyword(k => k.ResourceId)
                        .Keyword(k => k.ServiceName)
                        .Date(d => d.Timestamp))));

            if (!createResponse.IsValidResponse)
                _logger.LogWarning("Failed to create Elasticsearch index: {Error}", createResponse.DebugInformation);
            else
                _logger.LogInformation("Created Elasticsearch index: {IndexName}", IndexName);
        }
    }

    public async Task IndexAsync(AuditEvent auditEvent)
    {
        var response = await _client.IndexAsync(auditEvent, i => i.Index(IndexName).Id(auditEvent.EventId.ToString()));
        if (!response.IsValidResponse)
            _logger.LogWarning("Failed to index audit event {EventId}: {Error}", auditEvent.EventId, response.DebugInformation);
    }

    public async Task<(IList<AuditEventDto> Items, long Total)> SearchAsync(AuditSearchRequest request)
    {
        var response = await _client.SearchAsync<AuditEvent>(s => s
            .Index(IndexName)
            .From((request.Page - 1) * request.Size)
            .Size(request.Size)
            .Query(q => BuildQuery(request))
            .Sort(sort => sort.Field("timestamp"!, new FieldSort { Order = SortOrder.Desc })));

        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Elasticsearch search failed: {response.DebugInformation}");

        var items = response.Hits.Select(h => new AuditEventDto
        {
            EventId = h.Source!.EventId,
            EventType = h.Source.EventType,
            UserId = h.Source.UserId,
            ServiceName = h.Source.ServiceName,
            Action = h.Source.Action,
            ResourceType = h.Source.ResourceType,
            ResourceId = h.Source.ResourceId,
            OldValue = h.Source.OldValue,
            NewValue = h.Source.NewValue,
            IpAddress = h.Source.IpAddress,
            CorrelationId = h.Source.CorrelationId,
            Timestamp = h.Source.Timestamp,
            Metadata = h.Source.Metadata
        }).ToList();

        return (items, response.Total);
    }

    private static Query BuildQuery(AuditSearchRequest request)
    {
        var queries = new List<Query>();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            queries.Add(new MultiMatchQuery
            {
                Query = request.Query,
                Fields = new[] { "eventType", "action", "resourceType", "serviceName" }
            });
        }

        if (request.UserId.HasValue)
            queries.Add(new TermQuery("userId"!) { Value = request.UserId.Value.ToString() });

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
            queries.Add(new TermQuery("serviceName"!) { Value = request.ServiceName });

        if (request.From.HasValue || request.To.HasValue)
        {
            var rangeQuery = new DateRangeQuery("timestamp"!);
            if (request.From.HasValue) rangeQuery.Gte = request.From.Value;
            if (request.To.HasValue) rangeQuery.Lte = request.To.Value;
            queries.Add(rangeQuery);
        }

        if (queries.Count == 0)
            return new MatchAllQuery();

        return new BoolQuery { Must = queries };
    }
}
