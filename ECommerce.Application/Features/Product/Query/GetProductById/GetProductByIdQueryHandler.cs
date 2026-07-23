using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Product;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Products.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Query.GetProductById;

public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ErrorOr<ProductResponseDto>>
{
    private readonly IRepository<Domain.Entities.Product> _repository;

    public GetProductByIdQueryHandler(IRepository<Domain.Entities.Product> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<ProductResponseDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);

        if (product is null)
        {
            return Error.NotFound("Product.NotFound", $"Product with Id '{request.Id}' was not found.");
        }

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