using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Query.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<ErrorOr<ProductResponseDto>>;