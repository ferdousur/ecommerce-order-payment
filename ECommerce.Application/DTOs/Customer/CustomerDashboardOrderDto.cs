namespace ECommerce.Application.DTOs.Dashboard;

public class CustomerDashboardOrderDto
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public int TotalItems { get; set; }

    public CustomerPaymentInfoDto? Payment { get; set; }
}

public class CustomerPaymentInfoDto
{
    public Guid PaymentId { get; set; }
    public string Provider { get; set; } = string.Empty; // e.g., Bkash, Nagad, Stripe
    public string PaymentStatus { get; set; } = string.Empty; // e.g., Pending, Completed
    public string? TransactionId { get; set; }
    public DateTime PaymentDate { get; set; }
}