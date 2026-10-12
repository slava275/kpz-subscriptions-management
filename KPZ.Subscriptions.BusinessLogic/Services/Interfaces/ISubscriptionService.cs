using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface ISubscriptionService
{
    Task<PagedDto<SubscriptionListItemDto>> SearchAsync(SubscriptionQuery query, CancellationToken ct);
    Task<SubscriptionDetailsDto> GetAsync(int id, CancellationToken ct);
    Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto, CancellationToken ct);
    Task<SubscriptionDto> UpdateAsync(int id, UpdateSubscriptionDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<SubscriptionListItemDto>> GetUpcomingAsync(int userId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<SubscriptionListItemDto>> GetRemindersAsync(DateOnly today, CancellationToken ct);
    Task<IReadOnlyList<CategorySpendingDto>> GetSpendingAsync(int userId, int minCount, CancellationToken ct);
}
