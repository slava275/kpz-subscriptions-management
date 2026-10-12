using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class TagService(
    ITagRepository tags,
    IUserRepository users,
    ISubscriptionRepository subscriptions,
    IValidator<CreateTagDto> createValidator,
    IValidator<AttachTagDto> attachValidator) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetByUserAsync(int userId, CancellationToken ct)
    {
        _ = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Користувача", userId);
        return (await tags.GetByUserAsync(userId, ct)).Select(t => t.ToDto()).ToList();
    }

    public async Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        _ = await users.GetByIdAsync(dto.UserId, ct) ?? throw new NotFoundException("Користувача", dto.UserId);

        var name = dto.Name.Trim();
        if (await tags.ExistsAsync(dto.UserId, name, ct))
            throw new DuplicateException($"Тег «{name}» уже існує.");

        var id = await tags.AddAsync(new Tag { UserId = dto.UserId, Name = name }, ct);
        return (await tags.GetByIdAsync(id, ct))!.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await tags.DeleteAsync(id, ct)) throw new NotFoundException("Тег", id);
    }

    public async Task<IReadOnlyList<TagDto>> GetBySubscriptionAsync(int subscriptionId, CancellationToken ct)
    {
        _ = await subscriptions.GetByIdAsync(subscriptionId, ct) ?? throw new NotFoundException("Підписку", subscriptionId);
        return (await tags.GetBySubscriptionAsync(subscriptionId, ct)).Select(t => t.ToDto()).ToList();
    }

    public async Task AttachAsync(int subscriptionId, int tagId, AttachTagDto dto, CancellationToken ct)
    {
        await attachValidator.ValidateAndThrowAsync(dto, ct);
        var sub = await subscriptions.GetByIdAsync(subscriptionId, ct) ?? throw new NotFoundException("Підписку", subscriptionId);
        var tag = await tags.GetByIdAsync(tagId, ct) ?? throw new NotFoundException("Тег", tagId);

        if (tag.UserId != sub.UserId)
            throw new BusinessRuleException("Тег належить іншому користувачу.");

        await tags.AttachAsync(subscriptionId, tagId, dto.Note, ct);   // дубль -> DuplicateException
    }

    public async Task DetachAsync(int subscriptionId, int tagId, CancellationToken ct)
    {
        if (!await tags.DetachAsync(subscriptionId, tagId, ct))
            throw new NotFoundException("Зв'язок підписки з тегом", $"{subscriptionId}/{tagId}");
    }
}