namespace KPZ.Subscriptions.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
}
