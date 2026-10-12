namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record CategoryDto(int Id, string Name);
public sealed record CreateCategoryDto(string Name);
public sealed record UpdateCategoryDto(string Name);
