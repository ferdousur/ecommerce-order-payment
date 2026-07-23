
using ECommerce.Infrastructure.Identity.Services;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using ECommerce.Infrastructure.DbContext;
using ECommerce.Infrastructure.Identity;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure;

public static class DependencyInjections
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("ECommerce.Infrastructure"));
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}