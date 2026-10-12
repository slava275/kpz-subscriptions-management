using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Models;

public sealed record SubscriptionFilter(
    int? UserId = null,
    int? CategoryId = null,
    SubscriptionStatus? Status = null,
    string? Search = null,
    decimal? MinAmount = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 20);
