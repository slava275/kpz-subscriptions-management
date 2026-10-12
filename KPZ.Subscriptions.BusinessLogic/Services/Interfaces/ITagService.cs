using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetByUserAsync(int userId, CancellationToken ct);
    Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<TagDto>> GetBySubscriptionAsync(int subscriptionId, CancellationToken ct);
    Task AttachAsync(int subscriptionId, int tagId, AttachTagDto dto, CancellationToken ct);
    Task DetachAsync(int subscriptionId, int tagId, CancellationToken ct);
}