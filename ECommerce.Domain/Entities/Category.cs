namespace ECommerce.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    //Amr parent ache? 
    public Guid? ParentCategoryId { get; set; }
    //parent category navigation property
    public Category? ParentCategory { get; set; }
    //amr child kara 
    public ICollection<Category> ChildCategories { get; set; } = [];
    public ICollection<ProductCategory> ProductCategories { get; set; } = [];
}