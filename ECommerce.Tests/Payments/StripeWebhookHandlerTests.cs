using Microsoft.Extensions.Configuration;
using ECommerce.Infrastructure.Payments;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Payments;

public class StripeWebhookHandlerTests
{
    private readonly Mock<IRepository<Order>> _orderRepoMock = new();
    private readonly Mock<IRepository<Product>> _productRepoMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<StripeWebhookHandler>> _loggerMock = new();
    private readonly StripeWebhookHandler _handler;

    public StripeWebhookHandlerTests()
    {
        _configMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("whsec_test_secret");
        _handler = new StripeWebhookHandler(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvalidSignature_ReturnsFalse()
    {
        var result = await _handler.ProcessWebhookAsync("{}", "t=123,v1=badsignature", CancellationToken.None);
        result.Should().BeFalse();
    }
}