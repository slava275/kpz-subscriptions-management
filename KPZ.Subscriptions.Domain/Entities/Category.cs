namespace KPZ.Subscriptions.Domain.Entities;

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
