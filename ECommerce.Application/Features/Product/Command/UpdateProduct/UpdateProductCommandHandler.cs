using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ErrorOr<ProductResponseDto>>
{
    private readonly IRepository<Domain.Entities.Product> _repository;

    public UpdateProductCommandHandler(IRepository<Domain.Entities.Product> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<ProductResponseDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Entity DB-te ache kina check
        var product = await _repository.GetByIdAsync(request.Id);
        if (product is null)
        {
            return Error.NotFound("Product.NotFound", $"Product with Id '{request.Id}' was not found.");
        }

        // 2. Product field-gulo update kora
        product.Name = request.Name;
        product.Description = request.Description!;
        product.Price = request.Price;
        product.Stock = request.StockQuantity;
        product.Sku = request.SKU!;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;

        // 3. Update ebong Save Changes
        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        // 4. Response DTO return kora
        return new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.Sku,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name,
            product.CreatedAt
        );
    }
}