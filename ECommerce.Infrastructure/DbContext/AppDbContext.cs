using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Domain.Entities;

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

        // One-to-Many: UserProfile -> Carts (fixed: was One-to-One)
        builder.Entity<Cart>()
            .HasOne(c => c.UserProfile)
            .WithMany(up => up.Carts)
            .HasForeignKey(c => c.UserProfileId);

        // One-to-Many: Cart -> CartItems
        builder.Entity<Cart>()
            .HasMany(c => c.CartItems)
            .WithOne(ci => ci.Cart)
            .HasForeignKey(ci => ci.CartId);

        // Many-to-One: CartItem -> Product
        builder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(ci => ci.ProductId);

        // Unique: একই Cart-এ একই Product দুইবার row হবে না
        builder.Entity<CartItem>()
            .HasIndex(ci => new { ci.CartId, ci.ProductId })
            .IsUnique();

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

        // Many-to-One: OrderItem -> Product
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId);

        // One-to-One: Order -> Payment
        builder.Entity<Order>()
            .HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId);


        //composite key 
        builder.Entity<ProductCategory>()
            .HasKey(cp => new { cp.CategoryId, cp.ProductId });


        //Many ProductCategory to One Category
        builder.Entity<ProductCategory>()
            .HasOne(cp => cp.Category)
            .WithMany(c => c.ProductCategories)
            .HasForeignKey(cp => cp.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        //Many ProductCategory to One Product
        builder.Entity<ProductCategory>()
            .HasOne(cp => cp.Product)
            .WithMany(p => p.ProductCategories)
            .HasForeignKey(cp => cp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);



        // Unique Constraint for Product SKU
        builder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        // Unique Constraint for Payment Transaction Id
        builder.Entity<Payment>()
            .HasIndex(p => p.TransactionId)
            .IsUnique();

        builder.Entity<Product>(entity =>
         {
             entity.Property(p => p.RowVersion)
                 .IsRowVersion();
         });
    }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }

}