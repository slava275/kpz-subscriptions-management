namespace KPZ.Subscriptions.Domain.Entities;

public sealed class ExternalLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Provider { get; set; }
    public required string ProviderSubject { get; set; }
    public string? ProviderEmail { get; set; }
    public DateTime LinkedAt { get; set; }
}