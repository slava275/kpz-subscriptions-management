namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record ExternalIdentityDto(string Provider, string Subject, string Email,
    bool EmailVerified, string? Name, string? AvatarUrl);
