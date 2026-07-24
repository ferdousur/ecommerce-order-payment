using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

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
        // Category/ProductCategories Include করে Data Fetch করা
        var products = await _repository.GetQueryable()
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .AsNoTracking() // Read-only query performance optimize করার জন্য
            .ToListAsync(cancellationToken);

        var productDtos = products.Select(product => new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.Sku,
            product.IsActive,
            product.ProductCategories.Select(pc => pc.Category!.Name).ToList(), // Category Names List
            product.CreatedAt
        )).ToList();

        return productDtos;
    }
}