using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record ChargeDto(int Id, int SubscriptionId, DateOnly DueDate, decimal Amount,
    ChargeStatus Status, DateTime? PaidAt);
public sealed record CreateChargeDto(int SubscriptionId, DateOnly DueDate, decimal Amount);
