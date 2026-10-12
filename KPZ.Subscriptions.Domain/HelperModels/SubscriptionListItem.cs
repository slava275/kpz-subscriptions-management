using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Models;

public sealed record SubscriptionListItem(
    int Id, string Name, string CategoryName, string PaymentMethodTitle,
    decimal Amount, string Currency, BillingPeriod Period,
    DateOnly NextChargeDate, SubscriptionStatus Status);
