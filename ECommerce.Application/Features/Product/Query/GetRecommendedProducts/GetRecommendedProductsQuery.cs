using ErrorOr;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Features.Products.Queries.GetRecommendedProducts;

public record GetRecommendedProductsQuery(int Limit = 10) : IQuery<ErrorOr<List<RecommendedProductDto>>>;