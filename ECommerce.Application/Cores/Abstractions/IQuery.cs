using MediatR;

namespace ECommerce.Application.Cores.Abstractions;

public interface IQuery<TResponse> : IRequest<TResponse> { };
public interface IQuery : IRequest;
