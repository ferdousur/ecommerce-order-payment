

using ECommerce.Application.Cores.Behaviours;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class DependencyInjections
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IUserService).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
       
        return services;
    }
}