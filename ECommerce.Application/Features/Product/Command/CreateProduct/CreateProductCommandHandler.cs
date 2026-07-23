using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Product;
using ECommerce.Application.Features.Category.Command.CreateProduct;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ErrorOr<CreateProductDto>>
{
    private readonly IRepository<Domain.Entities.Product> _repository;

    public CreateProductCommandHandler(IRepository<Domain.Entities.Product> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CreateProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Create Domain Entity from Command
        var product = new Domain.Entities.Product
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Description = request.Description!,
            Price = request.Price,
            Stock = request.StockQuantity,
            Sku = request.SKU!,
            IsActive = request.IsActive,
            CategoryId = request.CategoryId
        };

        // 2. Persist to Database
        var createdProduct = await _repository.CreateAsync(product);
        await _repository.SaveChangesAsync();

        // 3. Map and Return CreateProductDto
        return new CreateProductDto(
           createdProduct.Name,
            createdProduct.Description,
            createdProduct.Price,
            createdProduct.Stock,
            createdProduct.Sku,
            createdProduct.CategoryId,
            createdProduct.IsActive
        );
    }
}