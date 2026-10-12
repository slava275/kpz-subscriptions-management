using KPZ.Subscriptions.Domain.Entities;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Tag>> GetByUserAsync(int userId, CancellationToken ct);
    Task<bool> ExistsAsync(int userId, string name, CancellationToken ct);
    Task<int> AddAsync(Tag tag, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    Task AttachAsync(int subscriptionId, int tagId, string? note, CancellationToken ct);
    Task<bool> DetachAsync(int subscriptionId, int tagId, CancellationToken ct);
    Task<IReadOnlyList<Tag>> GetBySubscriptionAsync(int subscriptionId, CancellationToken ct);
}
