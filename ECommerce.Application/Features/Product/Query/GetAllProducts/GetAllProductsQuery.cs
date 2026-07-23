using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Query.GetAllProducts;

public record GetAllProductsQuery() : IQuery<ErrorOr<List<ProductResponseDto>>>;