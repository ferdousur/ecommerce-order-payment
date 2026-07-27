using ECommerce.Domain.Entities;

using FluentAssertions;

namespace ECommerce.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void ProductShoudCreateProduct()
    {

        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var price = 99.99m;


        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Test Product Description",
            Price = price,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        product.Should().NotBeNull();
        product.Id.Should().Be(productId);
        product.Name.Should().Be("Test Product");
        product.Price.Should().Be(price);
    }

    [Fact]
    public void Product_ShouldUpdateProductPrice()
    {

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Product",
            Price = 10.00m
        };
        var newPrice = 15.50m;

        //    Act
        product.Price = newPrice;

        //   Assert
        product.Price.Should().Be(newPrice);
    }
}