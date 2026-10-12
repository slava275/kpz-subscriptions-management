using KPZ.Subscriptions.Domain.Enums;

namespace Kpz.Subscriptions.Domain.Models;

public static class BillingPeriodExtensions
{
    public static DateOnly Advance(this BillingPeriod period, DateOnly date) => period switch
    {
        BillingPeriod.Weekly => date.AddDays(7),
        BillingPeriod.Monthly => date.AddMonths(1),
        BillingPeriod.Quarterly => date.AddMonths(3),
        BillingPeriod.Yearly => date.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };
}
