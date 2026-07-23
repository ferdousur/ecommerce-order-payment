using MediatR;

namespace ECommerce.Application.Cores.Abstractions;

public interface ICommand<TResponse> : IRequest<TResponse> { };
public interface ICommand : IRequest;
