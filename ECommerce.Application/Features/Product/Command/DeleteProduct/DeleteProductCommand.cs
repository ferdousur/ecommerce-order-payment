using ECommerce.Application.Cores.Abstractions;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.DeleteProduct;

public record DeleteProductCommand(Guid Id) : ICommand<ErrorOr<Deleted>>;