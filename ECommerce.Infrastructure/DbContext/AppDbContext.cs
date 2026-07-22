using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.DbContext;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        // Rename default Identity tables
        builder.Entity<ApplicationUser>().ToTable("ECommerceUser");
        builder.Entity<IdentityRole<Guid>>().ToTable("ECommerceRole");

        // One-to-One: ApplicationUser -> UserProfile
        builder.Entity<UserProfile>()
            .HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(x => x.ApplicationUserId);


        // One-to-One: UserProfile -> Cart
        builder.Entity<UserProfile>()
             .HasOne(up => up.Cart)
             .WithOne(c => c.UserProfile)
             .HasForeignKey<Cart>(c => c.UserProfileId);

        // One-to-Many: Cart -> CartItems
        builder.Entity<Cart>()
            .HasMany(c => c.CartItems)
            .WithOne(ci => ci.Cart)
            .HasForeignKey(ci => ci.CartId);

        // One-to-Many: Product -> CartItems (Many-to-Many via CartItem junction)
        builder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(ci => ci.ProductId);



        // One-to-Many: UserProfile -> Orders
        builder.Entity<Order>()
                 .HasOne(o => o.UserProfile)
                 .WithMany(up => up.Orders)
                 .HasForeignKey(o => o.UserProfileId);

        // One-to-Many: Order -> OrderItems
        builder.Entity<Order>()
            .HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId);

        // One-to-Many: Product -> OrderItems (Many-to-Many via OrderItem junction)
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId);


        // One-to-One: Order -> Payment
        builder.Entity<Order>()
            .HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId);

        // Unique Constraint for Product SKU
        builder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        // Unique Constraint for Payment Transaction Id
        builder.Entity<Payment>()
                .HasIndex(t => t.TransactionId)
                .IsUnique();
    }


    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
}