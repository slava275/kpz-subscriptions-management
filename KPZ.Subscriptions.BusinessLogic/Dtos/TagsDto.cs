namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record TagDto(int Id, int UserId, string Name);
public sealed record CreateTagDto(int UserId, string Name);
public sealed record AttachTagDto(string? Note);
