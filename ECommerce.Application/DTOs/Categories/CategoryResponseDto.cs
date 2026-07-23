namespace ECommerce.Application.Categories.DTOs;

public record CategoryResponseDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc
);