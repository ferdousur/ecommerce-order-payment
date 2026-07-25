using ECommerce.Api;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Seeding;

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

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}


app.UseAuthentication();
app.UseAuthorization();


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

