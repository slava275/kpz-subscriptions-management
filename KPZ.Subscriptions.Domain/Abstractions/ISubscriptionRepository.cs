using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Models;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(int id, CancellationToken ct);
    Task<Subscription?> GetWithDetailsAsync(int id, CancellationToken ct);
    Task<int> AddAsync(Subscription subscription, CancellationToken ct);
    Task<bool> UpdateAsync(Subscription subscription, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    Task<PagedResult<SubscriptionListItem>> SearchAsync(SubscriptionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SubscriptionListItem>> GetUpcomingAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<SubscriptionListItem>> GetDueForReminderAsync(
        DateOnly today, CancellationToken ct);

    Task<IReadOnlyList<CategorySpendingRow>> GetSpendingByCategoryAsync(
        int userId, int minSubscriptionCount, CancellationToken ct);
}
