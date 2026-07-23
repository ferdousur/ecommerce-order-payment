using MediatR;

namespace ECommerce.Application.Cores.Abstractions;

public interface ICommandHandler<TCommand, TResponse> :
        IRequestHandler<TCommand, TResponse> where TCommand : IRequest<TResponse>;
