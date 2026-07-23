namespace ECommerce.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public UserProfile? UserProfile { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}