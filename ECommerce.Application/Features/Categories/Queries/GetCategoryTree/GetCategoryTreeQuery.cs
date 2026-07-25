using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Categories;
using ErrorOr;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryTree;

public record GetCategoryTreeQuery() : IQuery<ErrorOr<List<CategoryTreeDto>>>;