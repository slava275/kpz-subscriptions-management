using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface IBillingRepository
{
    /// <summary>
    /// Атомарно: позначити Charge як Paid, посунути Subscription.NextChargeDate,
    /// створити наступний Charge (Planned). Усе або нічого.
    /// </summary>
    Task<PayChargeResult> PayChargeAsync(int chargeId, DateTime paidAtUtc, CancellationToken ct);
}
