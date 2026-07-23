using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ECommerce.Infrastructure.DbContext;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using ECommerce.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Roles
        if (!await roleManager.RoleExistsAsync(Roles.Customer))
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Customer));

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));

        // 2. Admin user + UserProfile
        var adminEmail = configuration["AdminSeed:Email"];
        var adminPassword = configuration["AdminSeed:Password"];

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail!);
        if (existingAdmin is null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = adminEmail,
                UserName = adminEmail
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword!);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);

                dbContext.UserProfiles.Add(new UserProfile
                {
                    Id = Guid.CreateVersion7(),
                    ApplicationUserId = adminUser.Id,
                    FirstName = "System",
                    LastName = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                await dbContext.SaveChangesAsync();
            }
        }

        // 3. Category + Product Sample Data Seeding
        if (!await dbContext.Categories.AnyAsync())
        {
            // Category Definitions (Electronics Sub-Categories)
            var smartphonesCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Smartphones & Mobile",
                Description = "Latest Android and iOS smartphones",
                IsActive = true
            };

            var laptopsCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Laptops & Computers",
                Description = "High performance laptops and desktop PCs",
                IsActive = true
            };

            var audioCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Audio & Headphones",
                Description = "Wireless earbuds, noise-canceling headphones, and speakers",
                IsActive = true,
            };

            var wearablesCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Wearable Tech",
                Description = "Smartwatches, fitness trackers, and smart rings",
                IsActive = true,
            };

            var accessoriesCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Computer Accessories",
                Description = "Keyboards, mice, monitors, and docks",
                IsActive = true,
            };

            var categories = new List<Category>
            {
                smartphonesCat,
                laptopsCat,
                audioCat,
                wearablesCat,
                accessoriesCat
            };

            await dbContext.Categories.AddRangeAsync(categories);

            // 30 Sample Electronics Products
            var products = new List<Product>
            {
                // --- Smartphones & Mobile ---
                new Product { Id = Guid.CreateVersion7(), Name = "iPhone 15 Pro Max", Description = "Titanium design, A17 Pro chip, 256GB Storage.", Price = 1199.99m, Stock = 25, Sku = "SP-IP15PM-256", CategoryId = smartphonesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Samsung Galaxy S24 Ultra", Description = "200MP Camera, S Pen included, Snapdragon 8 Gen 3.", Price = 1299.99m, Stock = 30, Sku = "SP-S24U-512", CategoryId = smartphonesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Google Pixel 8 Pro", Description = "Advanced AI camera features, Google Tensor G3.", Price = 899.99m, Stock = 20, Sku = "SP-PIX8P-128", CategoryId = smartphonesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "OnePlus 12", Description = "100W SuperVOOC charging, Snapdragon 8 Gen 3.", Price = 799.99m, Stock = 15, Sku = "SP-OP12-256", CategoryId = smartphonesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Xiaomi 14 Ultra", Description = "Leica quad-camera system, 5000mAh battery.", Price = 1099.99m, Stock = 10, Sku = "SP-XIA14U-512", CategoryId = smartphonesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "iPhone 14", Description = "Super Retina XDR display, dual-camera system.", Price = 699.99m, Stock = 40, Sku = "SP-IP14-128", CategoryId = smartphonesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },

                // --- Laptops & Computers ---
                new Product { Id = Guid.CreateVersion7(), Name = "MacBook Pro 16\" M3 Max", Description = "36GB Unified Memory, 1TB SSD, Liquid Retina XDR.", Price = 3499.99m, Stock = 8, Sku = "LP-MBP16-M3", CategoryId = laptopsCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Dell XPS 15", Description = "Intel Core i9, 32GB RAM, RTX 4060, OLED Touch display.", Price = 2199.99m, Stock = 12, Sku = "LP-XPS15-i9", CategoryId = laptopsCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "ASUS ROG Zephyrus G14", Description = "AMD Ryzen 9, RTX 4070, 120Hz ROG Nebula Display.", Price = 1599.99m, Stock = 18, Sku = "LP-ROG-G14", CategoryId = laptopsCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Lenovo ThinkPad X1 Carbon", Description = "Ultralight business laptop, Intel Core Ultra 7.", Price = 1849.99m, Stock = 14, Sku = "LP-TP-X1C", CategoryId = laptopsCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "HP Spectre x360", Description = "2-in-1 convertible laptop, 14-inch 2.8K OLED screen.", Price = 1399.99m, Stock = 10, Sku = "LP-HP-SPEC14", CategoryId = laptopsCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "MacBook Air 13\" M2", Description = "8GB RAM, 256GB SSD, fanless quiet design.", Price = 999.99m, Stock = 35, Sku = "LP-MBA13-M2", CategoryId = laptopsCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },

                // --- Audio & Headphones ---
                new Product { Id = Guid.CreateVersion7(), Name = "Sony WH-1000XM5", Description = "Industry-leading noise canceling wireless headphones.", Price = 399.99m, Stock = 50, Sku = "AU-SONY-XM5", CategoryId = audioCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Apple AirPods Pro (2nd Gen)", Description = "USB-C charging case, Active Noise Cancellation.", Price = 249.99m, Stock = 60, Sku = "AU-APP2-USBC", CategoryId = audioCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Bose QuietComfort Ultra", Description = "Immersive spatial audio, world-class noise canceling.", Price = 429.99m, Stock = 22, Sku = "AU-BOSE-QCU", CategoryId = audioCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Sennheiser Momentum 4 Wireless", Description = "60-hour battery life, superior audiophile sound quality.", Price = 349.99m, Stock = 16, Sku = "AU-SENN-M4", CategoryId = audioCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "JBL Charge 5 Bluetooth Speaker", Description = "IP67 waterproof and dustproof, built-in powerbank.", Price = 179.99m, Stock = 45, Sku = "AU-JBL-CHG5", CategoryId = audioCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Sonos Move 2", Description = "Premium portable smart speaker with stereo sound.", Price = 449.99m, Stock = 11, Sku = "AU-SONOS-MV2", CategoryId = audioCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },

                // --- Wearable Tech ---
                new Product { Id = Guid.CreateVersion7(), Name = "Apple Watch Ultra 2", Description = "Rugged 49mm titanium case, precision dual-frequency GPS.", Price = 799.99m, Stock = 19, Sku = "WR-AWU2-49", CategoryId = wearablesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Samsung Galaxy Watch 6 Classic", Description = "Rotating bezel, bioelectrical impedance analysis sensor.", Price = 399.99m, Stock = 28, Sku = "WR-GW6C-47", CategoryId = wearablesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Garmin Fenix 7 Pro Sapphire Solar", Description = "Multisport GPS watch with built-in LED flashlight.", Price = 899.99m, Stock = 9, Sku = "WR-GAR-F7P", CategoryId = wearablesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Fitbit Charge 6", Description = "Advanced fitness tracker with heart rate and built-in GPS.", Price = 159.99m, Stock = 40, Sku = "WR-FIT-CH6", CategoryId = wearablesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Oura Ring Gen3", Description = "Smart ring for sleep tracking, readiness, and heart rate.", Price = 299.99m, Stock = 15, Sku = "WR-OURA-G3", CategoryId = wearablesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Apple Watch Series 9", Description = "Double tap gesture support, brighter Always-On Retina display.", Price = 399.99m, Stock = 32, Sku = "WR-AWS9-45", CategoryId = wearablesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },

                // --- Computer Accessories ---
                new Product { Id = Guid.CreateVersion7(), Name = "Logitech MX Master 3S Mouse", Description = "8K DPI tracking, quiet clicks, ergonomic wireless design.", Price = 99.99m, Stock = 75, Sku = "AC-LOGI-MX3S", CategoryId = accessoriesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Keychron K2 Pro Mechanical Keyboard", Description = "Wireless custom mechanical keyboard with QMK/VIA support.", Price = 119.99m, Stock = 50, Sku = "AC-KEY-K2PRO", CategoryId = accessoriesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Dell UltraSharp 27\" 4K Monitor (U2723QE)", Description = "IPS Black technology, USB-C Hub with 90W power delivery.", Price = 579.99m, Stock = 14, Sku = "AC-DELL-U2723", CategoryId = accessoriesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Samsung T7 Shield 2TB External SSD", Description = "Rugged portable SSD, up to 1050 MB/s read speed.", Price = 169.99m, Stock = 65, Sku = "AC-SAM-T7S-2T", CategoryId = accessoriesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Elgato Stream Deck MK.2", Description = "15 customizable LCD keys for studio control.", Price = 149.99m, Stock = 20, Sku = "AC-ELG-SDMK2", CategoryId = accessoriesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.CreateVersion7(), Name = "Anker 737 Power Bank 24,000mAh", Description = "140W fast charging output, smart digital display.", Price = 129.99m, Stock = 42, Sku = "AC-ANK-737", CategoryId = accessoriesCat.Id, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await dbContext.Products.AddRangeAsync(products);
            await dbContext.SaveChangesAsync();
        }
    }
}