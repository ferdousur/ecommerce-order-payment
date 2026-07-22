using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

}