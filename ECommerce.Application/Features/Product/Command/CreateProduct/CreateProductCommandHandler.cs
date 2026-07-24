using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Products.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ErrorOr<ProductResponseDto>>
{
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;

    public CreateProductCommandHandler(
        IRepository<Domain.Entities.Product> productRepository,
        IRepository<Domain.Entities.Category> categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ErrorOr<ProductResponseDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate CategoryIds List is not empty
        if (request.CategoryIds is null || !request.CategoryIds.Any())
        {
            return Error.Validation(description: "At least one category must be selected.");
        }

        // 2. Fetch all matching categories from DB
        var categories = await _categoryRepository.GetAsync(c => request.CategoryIds.Contains(c.Id));

        if (categories.Count() != request.CategoryIds.Distinct().Count())
        {
            return Error.NotFound(description: "One or more specified categories were not found.");
        }

        // 3. Create Product Instance
        var productId = Guid.CreateVersion7();
        var product = new Domain.Entities.Product
        {
            Id = productId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            Stock = request.StockQuantity,
            Sku = request.SKU ?? string.Empty,
            IsActive = request.IsActive,

            // 4. Populate Many-to-Many Join Table (ProductCategories)
            ProductCategories = categories.Select(c => new Domain.Entities.ProductCategory
            {
                ProductId = productId,
                CategoryId = c.Id
            }).ToList()
        };

        // 5. Save Product & Join Entities to DB
        var created = await _productRepository.CreateAsync(product);
        await _productRepository.SaveChangesAsync();

        // 6. Return Response DTO
        return new ProductResponseDto(
            created.Id,
            created.Name,
            created.Description,
            created.Price,
            created.Stock,
            created.Sku,
            created.IsActive,
            categories.Select(c => c.Name).ToList(),
            created.CreatedAt
        );
    }
}