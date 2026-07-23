using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.DeleteProduct;

public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, ErrorOr<Deleted>>
{
    private readonly IRepository<Domain.Entities.Product> _repository;

    public DeleteProductCommandHandler(IRepository<Domain.Entities.Product> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Entity DB-te ache kina check
        var product = await _repository.GetByIdAsync(request.Id);
        if (product is null)
        {
            return Error.NotFound("Product.NotFound", $"Product with Id '{request.Id}' was not found.");
        }

        // 2. Delete and Save changes
        // Note: Apnar repository overload onujayi (product) ba (product.Id) pass korun
        await _repository.DeleteAsync(product.Id);
        await _repository.SaveChangesAsync();

        return Result.Deleted;
    }
}