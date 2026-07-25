using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using CategoryEntity = ECommerce.Domain.Entities.Category;
using ProductEntity = ECommerce.Domain.Entities.Product;

namespace ECommerce.Application.Features.Products.Queries.GetRecommendedProducts;

public class GetRecommendedProductsQueryHandler
    : IQueryHandler<GetRecommendedProductsQuery, ErrorOr<List<RecommendedProductDto>>>
{
    private readonly IRepository<ProductEntity> _productRepository;
    private readonly IRepository<CategoryEntity> _categoryRepository;

    public GetRecommendedProductsQueryHandler(
        IRepository<ProductEntity> productRepository,
        IRepository<CategoryEntity> categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ErrorOr<List<RecommendedProductDto>>> Handle(
        GetRecommendedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var categoryIdsToSearch = new List<Guid>();

        // 1. If CategoryId is provided, use DFS to collect the category & all its sub-category IDs
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

        // 2. Query products using the collected category IDs
        var query = _productRepository.GetQueryable()
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryIdsToSearch.Any())
        {
            query = query.Where(p => categoryIdsToSearch.Contains(p.CategoryId));
        }

        // 3. Project to DTO and take top recommendation limit
        var recommendedProducts = await query
            .OrderByDescending(p => p.CreatedAt) // Or order by popularity/sales
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

        return recommendedProducts;
    }

    /// <summary>
    /// Recursive DFS to collect parent category ID and all nested sub-category IDs.
    /// </summary>
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