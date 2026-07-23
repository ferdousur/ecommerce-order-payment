using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Query.GetAllProducts;

public class GetAllProductsQueryHandler : IQueryHandler<GetAllProductsQuery, ErrorOr<List<ProductResponseDto>>>
{
    private readonly IRepository<Domain.Entities.Product> _repository;

    public GetAllProductsQueryHandler(IRepository<Domain.Entities.Product> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<List<ProductResponseDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetAllAsync();

        var productDtos = products.Select(product => new ProductResponseDto(
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
        )).ToList();

        return productDtos;
    }
}