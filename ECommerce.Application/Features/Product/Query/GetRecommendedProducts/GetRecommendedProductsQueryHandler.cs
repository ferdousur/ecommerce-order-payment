using System.Text.Json;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using CategoryEntity = ECommerce.Domain.Entities.Category;
using ProductEntity = ECommerce.Domain.Entities.Product;

namespace ECommerce.Application.Features.Products.Queries.GetRecommendedProducts;

public class GetRecommendedProductsQueryHandler
    : IQueryHandler<GetRecommendedProductsQuery, ErrorOr<List<RecommendedProductDto>>>
{
    private readonly IRepository<ProductEntity> _productRepository;
    private readonly IRepository<CategoryEntity> _categoryRepository;
    private readonly IDistributedCache _cache;

    public GetRecommendedProductsQueryHandler(
        IRepository<ProductEntity> productRepository,
        IRepository<CategoryEntity> categoryRepository,
        IDistributedCache cache)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<ErrorOr<List<RecommendedProductDto>>> Handle(
        GetRecommendedProductsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Dynamic Cache Key based on categoryId and limit
        string cacheKey = $"recommended_products_{request.CategoryId?.ToString() ?? "all"}_{request.Limit}";

        // 2. Try fetching from Redis Cache
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedProducts = JsonSerializer.Deserialize<List<RecommendedProductDto>>(cachedData);
            if (cachedProducts != null)
            {
                return cachedProducts;
            }
        }

        // 3. Cache Miss: Execute DFS & Database Query
        var categoryIdsToSearch = new List<Guid>();

        if (request.CategoryId.HasValue)
        {
            var allCategories = await _categoryRepository.GetQueryable()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var rootCategory = allCategories.FirstOrDefault(c => c.Id == request.CategoryId.Value);

            if (rootCategory != null)
            {
                CollectCategoryIdsDFS(rootCategory, allCategories, categoryIdsToSearch);
            }
        }

        var query = _productRepository.GetQueryable()
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryIdsToSearch.Any())
        {
            query = query.Where(p => categoryIdsToSearch.Contains(p.CategoryId));
        }

        var recommendedProducts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(request.Limit)
            .Select(p => new RecommendedProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        // 4. Store in Redis Cache for 15 Minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };

        var serializedData = JsonSerializer.Serialize(recommendedProducts);
        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

        return recommendedProducts;
    }

    private void CollectCategoryIdsDFS(
        CategoryEntity currentCategory,
        List<CategoryEntity> allCategories,
        List<Guid> categoryIds)
    {
        categoryIds.Add(currentCategory.Id);

        var children = allCategories
            .Where(c => c.ParentCategoryId == currentCategory.Id)
            .ToList();

        foreach (var child in children)
        {
            CollectCategoryIdsDFS(child, allCategories, categoryIds);
        }
    }
}