using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Cart;
using ErrorOr;

namespace ECommerce.Application.Features.Cart.Queries.GetMyCart;

public record GetMyCartQuery(Guid UserProfileId) : IQuery<ErrorOr<CartResponseDto>>;