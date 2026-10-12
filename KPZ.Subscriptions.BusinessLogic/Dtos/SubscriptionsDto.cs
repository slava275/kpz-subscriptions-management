using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record SubscriptionDto(int Id, string Name, string? Notes, decimal Amount, string Currency,
    BillingPeriod Period, DateOnly StartDate, DateOnly NextChargeDate, DateOnly? EndDate,
    int RemindBeforeDays, SubscriptionStatus Status, int UserId, int CategoryId, int PaymentMethodId,
    byte[] Version);

public sealed record CreateSubscriptionDto(string Name, string? Notes, decimal Amount, string Currency,
    BillingPeriod Period, DateOnly StartDate, DateOnly? EndDate, int RemindBeforeDays,
    int UserId, int CategoryId, int PaymentMethodId);

public sealed record UpdateSubscriptionDto(string Name, string? Notes, decimal Amount, string Currency,
    BillingPeriod Period, DateOnly StartDate, DateOnly? EndDate, int RemindBeforeDays,
    SubscriptionStatus Status, int CategoryId, int PaymentMethodId, byte[] Version);

public sealed record SubscriptionListItemDto(int Id, string Name, string CategoryName,
    string PaymentMethodTitle, decimal Amount, string Currency, BillingPeriod Period,
    DateOnly NextChargeDate, SubscriptionStatus Status);

public sealed record SubscriptionTagDto(int TagId, string Name, string? Note, DateTime AddedAt);

public sealed record SubscriptionDetailsDto(SubscriptionDto Subscription,
    IReadOnlyList<ChargeDto> Charges, IReadOnlyList<SubscriptionTagDto> Tags);

public sealed record CategorySpendingDto(int CategoryId, string CategoryName, int SubscriptionCount,
    decimal TotalAmount, decimal? AverageAmount);
