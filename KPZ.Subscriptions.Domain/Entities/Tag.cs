namespace KPZ.Subscriptions.Domain.Entities;

public sealed class Tag
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Name { get; set; }

    public ICollection<SubscriptionTag> SubscriptionTags { get; set; } = [];
}
