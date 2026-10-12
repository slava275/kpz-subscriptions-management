namespace KPZ.Subscriptions.Domain.Entities;

public sealed class SubscriptionTag
{
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public DateTime AddedAt { get; set; }
    public string? Note { get; set; }
}
