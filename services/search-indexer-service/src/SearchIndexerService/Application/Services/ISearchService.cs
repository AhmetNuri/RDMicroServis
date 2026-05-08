using SearchIndexerService.Domain;

namespace SearchIndexerService.Application.Services;

public interface ISearchService
{
    Task EnsureIndexExistsAsync();
    Task<(IList<ProductSearchDocument> Items, long Total)> SearchProductsAsync(
        string? query, string? category, decimal? minPrice, decimal? maxPrice, int page, int size);
    Task<ProductSearchDocument?> GetProductAsync(string id);
    Task IndexProductAsync(ProductSearchDocument document);
    Task DeleteProductAsync(string id);
}
