using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
}
