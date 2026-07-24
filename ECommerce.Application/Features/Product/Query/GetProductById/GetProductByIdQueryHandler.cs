using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Products.DTOs;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

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
        // 1. Fetch Product with Related Categories (Many-to-Many)
        var product = await _repository.GetQueryable()
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        // 2. Return Error if Not Found
        if (product is null)
        {
            return Error.NotFound("Product.NotFound", $"Product with Id '{request.Id}' was not found.");
        }

        // 3. Map to Response DTO
        return new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.Sku,
            product.IsActive,
            product.ProductCategories.Select(pc => pc.Category!.Name).ToList(),
            product.CreatedAt
        );
    }
}