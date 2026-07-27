using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ECommerce.Infrastructure.DbContext;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using ECommerce.Domain.Constants;
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

        // 1. Seed Roles
        if (!await roleManager.RoleExistsAsync(Roles.Customer))
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Customer));

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));

        // 2. Seed Admin User
        var adminEmail = configuration["AdminSeed:Email"] ?? "admin@ecommerce.com";
        var adminPassword = configuration["AdminSeed:Password"] ?? "Admin@123456";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
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

        // 3. Seed Categories & Products with Many-to-Many Relationships
        if (!await dbContext.Categories.AnyAsync())
        {
            // Main Parent Category
            var electronicsCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Electronics",
                Description = "All electronic devices, gadgets and accessories",
                IsActive = true
            };

            // Sub-Categories
            var smartphonesCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Smartphones & Mobile",
                Description = "Latest Android and iOS smartphones",
                IsActive = true,
                ParentCategoryId = electronicsCat.Id
            };

            var laptopsCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Laptops & Computers",
                Description = "High performance laptops and desktop PCs",
                IsActive = true,
                ParentCategoryId = electronicsCat.Id
            };

            var audioCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Audio & Headphones",
                Description = "Wireless earbuds, noise-canceling headphones, and speakers",
                IsActive = true,
                ParentCategoryId = electronicsCat.Id
            };

            var wearablesCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Wearable Tech",
                Description = "Smartwatches, fitness trackers, and smart rings",
                IsActive = true,
                ParentCategoryId = electronicsCat.Id
            };

            var accessoriesCat = new Category
            {
                Id = Guid.CreateVersion7(),
                Name = "Computer Accessories",
                Description = "Keyboards, mice, monitors, and docks",
                IsActive = true,
                ParentCategoryId = electronicsCat.Id
            };

            var categories = new List<Category>
            {
                electronicsCat,
                smartphonesCat,
                laptopsCat,
                audioCat,
                wearablesCat,
                accessoriesCat
            };

            // Step A: Save Categories first to establish FKs
            await dbContext.Categories.AddRangeAsync(categories);
            await dbContext.SaveChangesAsync();

            var productsList = new List<Product>();
            var productCategoriesList = new List<ProductCategory>();

            // Helper function with Distinct Check to avoid duplicates
            void AddProductWithCategories(string name, string desc, decimal price, int stock, string sku, params Category[] targetCategories)
            {
                var prodId = Guid.CreateVersion7();

                // Ensure target categories do not have duplicate entries for same product
                var uniqueCategories = targetCategories.DistinctBy(c => c.Id).ToList();

                var product = new Product
                {
                    Id = prodId,
                    Name = name,
                    Description = desc,
                    Price = price,
                    Stock = stock,
                    Sku = sku,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = uniqueCategories.First().Id // Primary Category
                };

                productsList.Add(product);

                foreach (var cat in uniqueCategories)
                {
                    productCategoriesList.Add(new ProductCategory
                    {
                        ProductId = prodId,
                        CategoryId = cat.Id
                    });
                }
            }

            // --- Smartphones & Mobile ---
            AddProductWithCategories("iPhone 15 Pro Max", "Titanium design, A17 Pro chip, 256GB Storage.", 1199.99m, 25, "SP-IP15PM-256", smartphonesCat, electronicsCat);
            AddProductWithCategories("Samsung Galaxy S24 Ultra", "200MP Camera, S Pen included, Snapdragon 8 Gen 3.", 1299.99m, 30, "SP-S24U-512", smartphonesCat, electronicsCat);
            AddProductWithCategories("Google Pixel 8 Pro", "Advanced AI camera features, Google Tensor G3.", 899.99m, 20, "SP-PIX8P-128", smartphonesCat, electronicsCat);
            AddProductWithCategories("OnePlus 12", "100W SuperVOOC charging, Snapdragon 8 Gen 3.", 799.99m, 15, "SP-OP12-256", smartphonesCat, electronicsCat);
            AddProductWithCategories("Xiaomi 14 Ultra", "Leica quad-camera system, 5000mAh battery.", 1099.99m, 10, "SP-XIA14U-512", smartphonesCat, electronicsCat);
            AddProductWithCategories("iPhone 14", "Super Retina XDR display, dual-camera system.", 699.99m, 40, "SP-IP14-128", smartphonesCat, electronicsCat);

            // --- Laptops & Computers ---
            AddProductWithCategories("MacBook Pro 16\" M3 Max", "36GB Unified Memory, 1TB SSD, Liquid Retina XDR.", 3499.99m, 8, "LP-MBP16-M3", laptopsCat, electronicsCat);
            AddProductWithCategories("Dell XPS 15", "Intel Core i9, 32GB RAM, RTX 4060, OLED Touch display.", 2199.99m, 12, "LP-XPS15-i9", laptopsCat, electronicsCat);
            AddProductWithCategories("ASUS ROG Zephyrus G14", "AMD Ryzen 9, RTX 4070, 120Hz ROG Nebula Display.", 1599.99m, 18, "LP-ROG-G14", laptopsCat, electronicsCat);
            AddProductWithCategories("Lenovo ThinkPad X1 Carbon", "Ultralight business laptop, Intel Core Ultra 7.", 1849.99m, 14, "LP-TP-X1C", laptopsCat, electronicsCat);
            AddProductWithCategories("HP Spectre x360", "2-in-1 convertible laptop, 14-inch 2.8K OLED screen.", 1399.99m, 10, "LP-HP-SPEC14", laptopsCat, electronicsCat);
            AddProductWithCategories("MacBook Air 13\" M2", "8GB RAM, 256GB SSD, fanless quiet design.", 999.99m, 35, "LP-MBA13-M2", laptopsCat, electronicsCat);

            // --- Audio & Headphones ---
            AddProductWithCategories("Sony WH-1000XM5", "Industry-leading noise canceling wireless headphones.", 399.99m, 50, "AU-SONY-XM5", audioCat, accessoriesCat, electronicsCat);
            AddProductWithCategories("Apple AirPods Pro (2nd Gen)", "USB-C charging case, Active Noise Cancellation.", 249.99m, 60, "AU-APP2-USBC", audioCat, electronicsCat);
            AddProductWithCategories("Bose QuietComfort Ultra", "Immersive spatial audio, world-class noise canceling.", 429.99m, 22, "AU-BOSE-QCU", audioCat, electronicsCat);
            AddProductWithCategories("Sennheiser Momentum 4 Wireless", "60-hour battery life, superior audiophile sound quality.", 349.99m, 16, "AU-SENN-M4", audioCat, electronicsCat);
            AddProductWithCategories("JBL Charge 5 Bluetooth Speaker", "IP67 waterproof and dustproof, built-in powerbank.", 179.99m, 45, "AU-JBL-CHG5", audioCat, electronicsCat);
            AddProductWithCategories("Sonos Move 2", "Premium portable smart speaker with stereo sound.", 449.99m, 11, "AU-SONOS-MV2", audioCat, electronicsCat);

            // --- Wearable Tech ---
            AddProductWithCategories("Apple Watch Ultra 2", "Rugged 49mm titanium case, precision dual-frequency GPS.", 799.99m, 19, "WR-AWU2-49", wearablesCat, electronicsCat);
            AddProductWithCategories("Samsung Galaxy Watch 6 Classic", "Rotating bezel, bioelectrical impedance analysis sensor.", 399.99m, 28, "WR-GW6C-47", wearablesCat, electronicsCat);
            AddProductWithCategories("Garmin Fenix 7 Pro Sapphire Solar", "Multisport GPS watch with built-in LED flashlight.", 899.99m, 9, "WR-GAR-F7P", wearablesCat, electronicsCat);
            AddProductWithCategories("Fitbit Charge 6", "Advanced fitness tracker with heart rate and built-in GPS.", 159.99m, 40, "WR-FIT-CH6", wearablesCat, electronicsCat);
            AddProductWithCategories("Oura Ring Gen3", "Smart ring for sleep tracking, readiness, and heart rate.", 299.99m, 15, "WR-OURA-G3", wearablesCat, electronicsCat);
            AddProductWithCategories("Apple Watch Series 9", "Double tap gesture support, brighter Always-On Retina display.", 399.99m, 32, "WR-AWS9-45", wearablesCat, electronicsCat);

            // --- Computer Accessories ---
            AddProductWithCategories("Logitech MX Master 3S Mouse", "8K DPI tracking, quiet clicks, ergonomic wireless design.", 99.99m, 75, "AC-LOGI-MX3S", accessoriesCat, electronicsCat);
            AddProductWithCategories("Keychron K2 Pro Mechanical Keyboard", "Wireless custom mechanical keyboard with QMK/VIA support.", 119.99m, 50, "AC-KEY-K2PRO", accessoriesCat, electronicsCat);
            AddProductWithCategories("Dell UltraSharp 27\" 4K Monitor (U2723QE)", "IPS Black technology, USB-C Hub with 90W power delivery.", 579.99m, 14, "AC-DELL-U2723", accessoriesCat, electronicsCat);
            AddProductWithCategories("Samsung T7 Shield 2TB External SSD", "Rugged portable SSD, up to 1050 MB/s read speed.", 169.99m, 65, "AC-SAM-T7S-2T", accessoriesCat, electronicsCat);
            AddProductWithCategories("Elgato Stream Deck MK.2", "15 customizable LCD keys for studio control.", 149.99m, 20, "AC-ELG-SDMK2", accessoriesCat, electronicsCat);
            AddProductWithCategories("Anker 737 Power Bank 24,000mAh", "140W fast charging output, smart digital display.", 129.99m, 42, "AC-ANK-737", accessoriesCat, electronicsCat);

            // Save Products and Join Tables
            await dbContext.Products.AddRangeAsync(productsList);
            await dbContext.Set<ProductCategory>().AddRangeAsync(productCategoriesList);
            await dbContext.SaveChangesAsync();
        }
    }
}