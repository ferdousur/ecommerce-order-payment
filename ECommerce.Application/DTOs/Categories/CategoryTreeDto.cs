namespace ECommerce.Application.DTOs.Categories;

public class CategoryTreeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public List<CategoryTreeDto> Children { get; set; } = new();
}