using MediatR;

namespace ECommerce.Application.Cores.Abstractions;

public interface IQueryHandler<TQuery, TResponse> :
        IRequestHandler<TQuery, TResponse> where TQuery : IRequest<TResponse>;
