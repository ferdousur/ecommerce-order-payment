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
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly ICurrentUserService _userContext;
    private readonly IDistributedCache _cache;

    public GetRecommendedProductsQueryHandler(
        IRepository<ProductEntity> productRepository,
        IRepository<CategoryEntity> categoryRepository,
        IRepository<Domain.Entities.Cart> cartRepository,
        ICurrentUserService userContext,
        IDistributedCache cache)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _cartRepository = cartRepository;
        _userContext = userContext;
        _cache = cache;
    }

    public async Task<ErrorOr<List<RecommendedProductDto>>> Handle(
        GetRecommendedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        var categoryIdsToSearch = new List<Guid>();
        bool hasCartItems = false;

        // 1. Check user cart and extract category IDs if items exist
        if (userId != null)
        {
            var userCart = await _cartRepository.GetQueryable()
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserProfileId == userId.Value, cancellationToken);

            if (userCart != null && userCart.CartItems.Any())
            {
                hasCartItems = true;


                var cartCategoryIds = userCart.CartItems
                    .Where(item => item.Product != null)
                    .Select(item => item.Product!.CategoryId)
                    .Distinct()
                    .ToList();

                var allCategories = await _categoryRepository.GetQueryable()
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Run DFS for each category found in the cart
                foreach (var catId in cartCategoryIds)
                {
                    var rootCategory = allCategories.FirstOrDefault(c => c.Id == catId);
                    if (rootCategory != null)
                    {
                        CollectCategoryIdsDFS(rootCategory, allCategories, categoryIdsToSearch);
                    }
                }
            }
        }

        // 2. Dynamic Cache Key based on whether cart has items or falling back to latest
        string cacheKey = hasCartItems
            ? $"recommended_cart_user_{userId}_{string.Join("_", categoryIdsToSearch.OrderBy(id => id))}_{request.Limit}"
            : $"recommended_latest_fallback_{request.Limit}";

        // 3. Try fetching from Redis Cache
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedProducts = JsonSerializer.Deserialize<List<RecommendedProductDto>>(cachedData);
            if (cachedProducts != null && cachedProducts.Any())
            {
                return cachedProducts;
            }
        }

        // 4. Cache Miss: Build Database Query
        var query = _productRepository.GetQueryable()
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (hasCartItems && categoryIdsToSearch.Any())
        {
            // Filter by DFS collected categories if cart has items
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

        // Store in Redis Cache for 15 Minutes
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
        if (!categoryIds.Contains(currentCategory.Id))
        {
            categoryIds.Add(currentCategory.Id);
        }

        var children = allCategories
            .Where(c => c.ParentCategoryId == currentCategory.Id)
            .ToList();

        foreach (var child in children)
        {
            CollectCategoryIdsDFS(child, allCategories, categoryIds);
        }
    }
}