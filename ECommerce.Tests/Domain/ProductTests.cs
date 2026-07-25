using System;
using System.Collections.Generic;

// আপনার প্রোজেক্টের আসল Namespace অনুযায়ী পরিবর্তন করুন
using ECommerce.Domain.Entities;

// টেস্টিং ও অ্যাসারশন ফ্রেমওয়ার্ক
using Xunit;
using FluentAssertions;

namespace ECommerce.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Product_বৈধ_তথ্য_দিয়ে_তৈরি_করা_সম্ভব_হওয়া_উচিত()
    {
        // ১. আরঞ্জ (Arrange): টেস্ট ডেটা তৈরি করা
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var price = 99.99m;

        // ২. অ্যাক্ট (Act): একটি নতুন Product অবজেক্ট তৈরি করা
        var product = new Product
        {
            Id = productId,
            Name = "টেস্ট প্রোডাক্ট",
            Description = "টেস্ট প্রোডাক্টের বিবরণ",
            Price = price,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        // ৩. অ্যাসার্ট (Assert): যাচাই করা যে প্রপার্টিগুলো ঠিকভাবে সেট হয়েছে কিনা

        product.Should().NotBeNull(); // প্রোডাক্ট অবজেক্টটি যেন নাল (Null) না হয়
        product.Id.Should().Be(productId); // ID ঠিক আছে কিনা চেক
        product.Name.Should().Be("টেস্ট প্রোডাক্ট"); // নাম ঠিক আছে কিনা চেক
        product.Price.Should().Be(price); // দাম ঠিক আছে কিনা চেক
    }

    [Fact]
    public void Product_এর_দাম_সঠিকভাবে_আপডেট_করা_সম্ভব_হওয়া_উচিত()
    {
        // ১. আরঞ্জ (Arrange): একটি প্রোডাক্ট অবজেক্ট তৈরি করা পুরনো দাম দিয়ে
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "পুরনো প্রোডাক্ট",
            Price = 10.00m
        };
        var newPrice = 15.50m; // নতুন দাম

        // ২. অ্যাক্ট (Act): প্রোডাক্টের দাম আপডেট করা
        product.Price = newPrice;

        // ৩. অ্যাসার্ট (Assert): যাচাই করা যে দাম আপডেট হয়েছে কিনা
        product.Price.Should().Be(newPrice);
    }
}