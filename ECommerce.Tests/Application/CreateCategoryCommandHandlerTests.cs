using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Features.Category.Command.CreateCategory;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace ECommerce.Tests.Application;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IRepository<Category>> _repositoryMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Category>>();
        _cacheMock = new Mock<IDistributedCache>();

        // IDistributedCache.RemoveAsync safe setup
        _cacheMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateCategoryCommandHandler(_repositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCategoryAndReturnDto()
    {
        // Arrange
        var command = new CreateCategoryCommand("Electronics", "Gadgets", null);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category cat) => cat); // Ensure created object is returned

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Electronics");
        result.Value.Description.Should().Be("Gadgets");

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("categories_tree_dfs", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenParentCategoryNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var command = new CreateCategoryCommand("Mobile", "Smartphones", parentId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(parentId))
            .ReturnsAsync((Category?)null); // Parent not found

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Category.ParentNotFound");

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Never);
    }
}