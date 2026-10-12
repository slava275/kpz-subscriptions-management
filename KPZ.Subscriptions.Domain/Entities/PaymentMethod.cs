using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Entities;

public sealed class PaymentMethod
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Title { get; set; }
    public PaymentMethodType Type { get; set; }
    public string? Last4 { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
