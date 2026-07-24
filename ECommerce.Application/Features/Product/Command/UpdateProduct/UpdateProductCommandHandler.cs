using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Products.DTOs;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Product.Command.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ErrorOr<ProductResponseDto>>
{
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;

    public UpdateProductCommandHandler(
        IRepository<Domain.Entities.Product> productRepository,
        IRepository<Domain.Entities.Category> categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ErrorOr<ProductResponseDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Product with Existing Categories
        var product = await _productRepository.GetQueryable()
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return Error.NotFound("Product.NotFound", $"Product with Id '{request.Id}' was not found.");
        }

        // 2. Validate New Category IDs
        if (request.CategoryIds is null || !request.CategoryIds.Any())
        {
            return Error.Validation("Product.CategoryRequired", "At least one category must be assigned.");
        }

        var categories = await _categoryRepository.GetQueryable()
            .Where(c => request.CategoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (categories.Count != request.CategoryIds.Distinct().Count())
        {
            return Error.NotFound("Category.NotFound", "One or more selected categories do not exist.");
        }

        // 3. Update Product Basic Fields
        product.Name = request.Name;
        product.Description = request.Description ?? string.Empty;
        product.Price = request.Price;
        product.Stock = request.StockQuantity;
        product.Sku = request.SKU ?? string.Empty;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        // 4. Update Many-to-Many Categories Junction Table
        product.ProductCategories.Clear(); // পুরাতন ক্যাটাগরি কানেকশনগুলো পরিষ্কার করা
        foreach (var category in categories)
        {
            product.ProductCategories.Add(new Domain.Entities.ProductCategory
            {
                ProductId = product.Id,
                CategoryId = category.Id
            });
        }

        // 5. Save Changes
        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        // 6. Return Updated DTO
        return new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.Sku,
            product.IsActive,
            categories.Select(c => c.Name).ToList(),
            product.CreatedAt
        );
    }
}