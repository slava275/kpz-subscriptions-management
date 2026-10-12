using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Models;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface IChargeRepository
{
    Task<Charge?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Charge>> GetBySubscriptionAsync(int subscriptionId, CancellationToken ct);
    Task<PagedResult<Charge>> GetPagedAsync(
        int subscriptionId, ChargeStatus? status, int page, int pageSize, CancellationToken ct);
    Task<int> AddAsync(Charge charge, CancellationToken ct);
    Task<bool> UpdateAsync(Charge charge, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
