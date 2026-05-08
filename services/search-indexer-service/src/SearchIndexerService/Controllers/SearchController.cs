using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchIndexerService.Application.Services;
using SearchIndexerService.Domain;

namespace SearchIndexerService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var (items, total) = await _searchService.SearchProductsAsync(q, category, minPrice, maxPrice, page, size);
        return Ok(new { data = items, total, page, size });
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        var product = await _searchService.GetProductAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost("products/index")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IndexProduct([FromBody] ProductSearchDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
            document.Id = Guid.NewGuid().ToString();
        await _searchService.IndexProductAsync(document);
        return Ok(new { message = "Product indexed", id = document.Id });
    }

    [HttpDelete("products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        await _searchService.DeleteProductAsync(id);
        return Ok(new { message = "Product deleted from index" });
    }
}
