using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using SearchIndexerService.Domain;

namespace SearchIndexerService.Application.Services;

public class SearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<SearchService> _logger;
    private const string IndexName = "products";

    public SearchService(IConfiguration configuration, ILogger<SearchService> logger)
    {
        _logger = logger;
        var uri = configuration["Elasticsearch__Uri"] ?? configuration["Elasticsearch:Uri"] ?? "http://elasticsearch:9200";
        var settings = new ElasticsearchClientSettings(new Uri(uri))
            .DefaultIndex(IndexName);
        _client = new ElasticsearchClient(settings);
    }

    public async Task EnsureIndexExistsAsync()
    {
        var existsResponse = await _client.Indices.ExistsAsync(IndexName);
        if (existsResponse.Exists) return;

        var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
            .Settings(s => s
                .Analysis(a => a
                    .Analyzers(an => an
                        .Custom("edge_ngram_analyzer", ca => ca
                            .Tokenizer("edge_ngram_tokenizer")
                            .Filter(["lowercase"])))
                    .Tokenizers(t => t
                        .EdgeNGram("edge_ngram_tokenizer", en => en
                            .MinGram(2)
                            .MaxGram(20)
                            .TokenChars([TokenChar.Letter, TokenChar.Digit])))))
            .Mappings(m => m
                .Properties<ProductSearchDocument>(p => p
                    .Keyword(d => d.Id)
                    .Text(d => d.Name, tf => tf
                        .Analyzer("edge_ngram_analyzer")
                        .SearchAnalyzer("standard")
                        .Boost(3))
                    .Text(d => d.Description, tf => tf
                        .Analyzer("edge_ngram_analyzer")
                        .SearchAnalyzer("standard"))
                    .Keyword(d => d.Category)
                    .Keyword(d => d.Tags)
                    .ScaledFloatNumber(d => d.Price, sf => sf.ScalingFactor(100))
                    .IntegerNumber(d => d.StockQuantity)
                    .Boolean(d => d.IsActive)
                    .Date(d => d.CreatedAt)
                    .Date(d => d.UpdatedAt))));

        if (!createResponse.IsValidResponse)
            _logger.LogWarning("Failed to create products index: {Error}", createResponse.DebugInformation);
        else
            _logger.LogInformation("Created products index with edge-ngram analyzer");
    }

    public async Task<(IList<ProductSearchDocument> Items, long Total)> SearchProductsAsync(
        string? query, string? category, decimal? minPrice, decimal? maxPrice, int page, int size)
    {
        var mustClauses = new List<Query>
        {
            new TermQuery(new Field("isActive")) { Value = true }
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            mustClauses.Add(new MultiMatchQuery
            {
                Query = query,
                Fields = new[] { "name^3", "description", "category", "tags" },
                Type = TextQueryType.BestFields,
                Fuzziness = new Fuzziness("AUTO")
            });
        }

        if (!string.IsNullOrWhiteSpace(category))
            mustClauses.Add(new TermQuery(new Field("category")) { Value = category });

        if (minPrice.HasValue || maxPrice.HasValue)
        {
            var rangeQuery = new NumberRangeQuery(new Field("price"));
            if (minPrice.HasValue) rangeQuery.Gte = (double)minPrice.Value;
            if (maxPrice.HasValue) rangeQuery.Lte = (double)maxPrice.Value;
            mustClauses.Add(rangeQuery);
        }

        var response = await _client.SearchAsync<ProductSearchDocument>(s => s
            .Index(IndexName)
            .From((page - 1) * size)
            .Size(size)
            .Query(new BoolQuery { Must = mustClauses }));

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Search failed: {Error}", response.DebugInformation);
            return ([], 0);
        }

        var items = response.Hits.Select(h => h.Source!).Where(s => s != null).ToList();
        return (items, response.Total);
    }

    public async Task<ProductSearchDocument?> GetProductAsync(string id)
    {
        var response = await _client.GetAsync<ProductSearchDocument>(id, g => g.Index(IndexName));
        return response.IsValidResponse ? response.Source : null;
    }

    public async Task IndexProductAsync(ProductSearchDocument document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        var response = await _client.IndexAsync(document, i => i.Index(IndexName).Id(document.Id));
        if (!response.IsValidResponse)
            _logger.LogWarning("Failed to index product {Id}: {Error}", document.Id, response.DebugInformation);
        else
            _logger.LogInformation("Indexed product {Id}: {Name}", document.Id, document.Name);
    }

    public async Task DeleteProductAsync(string id)
    {
        var response = await _client.DeleteAsync<ProductSearchDocument>(id, d => d.Index(IndexName));
        if (!response.IsValidResponse)
            _logger.LogWarning("Failed to delete product {Id}: {Error}", id, response.DebugInformation);
        else
            _logger.LogInformation("Deleted product {Id} from index", id);
    }
}
