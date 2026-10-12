using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Entities;

public sealed class Charge
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public ChargeStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
}
