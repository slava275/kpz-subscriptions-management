using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed class SubscriptionQuery
{
    public int? UserId { get; init; }
    public int? CategoryId { get; init; }
    public SubscriptionStatus? Status { get; init; }
    public string? Search { get; init; }
    public decimal? MinAmount { get; init; }
    public string? SortBy { get; init; }          // name | amount | category | (за замовчуванням дата)
    public bool Descending { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class ChargeQuery
{
    public int SubscriptionId { get; init; }
    public ChargeStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}