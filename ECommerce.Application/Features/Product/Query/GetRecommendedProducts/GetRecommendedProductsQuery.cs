using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Products;
using ErrorOr;

namespace ECommerce.Application.Features.Products.Queries.GetRecommendedProducts;

public record GetRecommendedProductsQuery(
    Guid? CategoryId,
    int Limit = 10
) : IQuery<ErrorOr<List<RecommendedProductDto>>>;