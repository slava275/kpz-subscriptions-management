using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record UserDto(int Id, string Name, string Email, UserRole Role, bool IsActive,
    DateTime CreatedAt, DateTime? LastLoginAt, string? AvatarUrl);
public sealed record CreateUserDto(string Name, string Email);
public sealed record UpdateUserDto(string Name, string Email);
