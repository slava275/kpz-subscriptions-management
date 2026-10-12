using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Entities;

public sealed class Subscription
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public BillingPeriod Period { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly NextChargeDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int RemindBeforeDays { get; set; }
    public SubscriptionStatus Status { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    public byte[] Version { get; set; } = [];

    public ICollection<Charge> Charges { get; set; } = [];
    public ICollection<SubscriptionTag> SubscriptionTags { get; set; } = [];
}
