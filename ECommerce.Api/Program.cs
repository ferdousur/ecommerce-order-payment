using ECommerce.Api;
using ECommerce.Api.Middlewares;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.DbContext;
using ECommerce.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

//Add Service Container
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHost(builder.Configuration);
builder.Services.AddApplication();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAll");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Create the database if it doesn't exist and apply any pending migrations automatically
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}


try
{
    await DbSeeder.SeedAsync(app);
    Console.WriteLine("--> Database Seeded Successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"--> Error Seeding Database: {ex.Message}");
}
app.Run();

