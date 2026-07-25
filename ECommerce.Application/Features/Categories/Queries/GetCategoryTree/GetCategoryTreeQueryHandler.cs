using System.Text.Json;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using CategoryEntity = ECommerce.Domain.Entities.Category;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryTree;

public class GetCategoryTreeQueryHandler
    : IQueryHandler<GetCategoryTreeQuery, ErrorOr<List<CategoryTreeDto>>>
{
    private readonly IRepository<CategoryEntity> _categoryRepository;
    private readonly IDistributedCache _cache;
    private const string CacheKey = "categories_tree_dfs";

    public GetCategoryTreeQueryHandler(
        IRepository<CategoryEntity> categoryRepository,
        IDistributedCache cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<ErrorOr<List<CategoryTreeDto>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Check Redis Cache first
        var cachedTree = await _cache.GetStringAsync(CacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedTree))
        {
            var deserializedTree = JsonSerializer.Deserialize<List<CategoryTreeDto>>(cachedTree);
            if (deserializedTree != null)
            {
                return deserializedTree;
            }
        }

        // 2. Cache Miss: Fetch all categories from DB in a single query
        List<CategoryEntity> allCategories = await _categoryRepository.GetQueryable()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 3. Identify root nodes (categories with no parent)
        var rootCategories = allCategories
            .Where(c => c.ParentCategoryId == null)
            .ToList();

        var treeResult = new List<CategoryTreeDto>();

        // 4. Run DFS algorithm to build hierarchy
        foreach (var root in rootCategories)
        {
            var nodeDto = TraverseDFS(root, allCategories);
            treeResult.Add(nodeDto);
        }

        // 5. Store result in Redis Cache (Expiration: 1 Hour)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        var serializedTree = JsonSerializer.Serialize(treeResult);
        await _cache.SetStringAsync(CacheKey, serializedTree, cacheOptions, cancellationToken);

        return treeResult;
    }

    /// <summary>
    /// Recursive Depth-First Search (DFS) traversal.
    /// </summary>
    private CategoryTreeDto TraverseDFS(CategoryEntity currentCategory, List<CategoryEntity> allCategories)
    {
        var dto = new CategoryTreeDto
        {
            Id = currentCategory.Id,
            Name = currentCategory.Name,
            ParentCategoryId = currentCategory.ParentCategoryId
        };

        var children = allCategories
            .Where(c => c.ParentCategoryId == currentCategory.Id)
            .ToList();

        foreach (var child in children)
        {
            var childDto = TraverseDFS(child, allCategories);
            dto.Children.Add(childDto);
        }

        return dto;
    }
}