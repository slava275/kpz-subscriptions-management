namespace KPZ.Subscriptions.Domain.Models;

public sealed record CategorySpendingRow(
    int CategoryId, string CategoryName, int SubscriptionCount,
    decimal TotalAmount, decimal? AverageAmount);
