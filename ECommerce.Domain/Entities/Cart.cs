namespace ECommerce.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

}