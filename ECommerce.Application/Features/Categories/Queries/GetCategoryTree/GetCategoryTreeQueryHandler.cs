using CategoryEntity = ECommerce.Domain.Entities.Category;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ErrorOr;



namespace ECommerce.Application.Features.Categories.Queries.GetCategoryTree;

public class GetCategoryTreeQueryHandler
    : IQueryHandler<GetCategoryTreeQuery, ErrorOr<List<CategoryTreeDto>>>
{
    private readonly IRepository<CategoryEntity> _categoryRepository;

    public GetCategoryTreeQueryHandler(IRepository<CategoryEntity> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ErrorOr<List<CategoryTreeDto>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Fetch all categories in a single query to optimize database calls
        List<CategoryEntity> allCategories = await _categoryRepository.GetQueryable()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 2. Identify root nodes (categories with no parent)
        var rootCategories = allCategories
            .Where(c => c.ParentCategoryId == null)
            .ToList();

        var treeResult = new List<CategoryTreeDto>();

        // 3. Initiate DFS traversal starting from each root node
        foreach (var root in rootCategories)
        {
            var nodeDto = TraverseDFS(root, allCategories);
            treeResult.Add(nodeDto);
        }

        return treeResult;
    }

    /// <summary>
    /// Recursive Depth-First Search (DFS) traversal to construct the category hierarchy tree.
    /// </summary>
    private CategoryTreeDto TraverseDFS(CategoryEntity currentCategory, List<CategoryEntity> allCategories)
    {
        var dto = new CategoryTreeDto
        {
            Id = currentCategory.Id,
            Name = currentCategory.Name,
            ParentCategoryId = currentCategory.ParentCategoryId
        };

        // Find direct child categories of the current node
        var children = allCategories
            .Where(c => c.ParentCategoryId == currentCategory.Id)
            .ToList();

        // Recursively traverse each child node down the depth of the tree
        foreach (var child in children)
        {
            var childDto = TraverseDFS(child, allCategories);
            dto.Children.Add(childDto);
        }

        return dto;
    }
}