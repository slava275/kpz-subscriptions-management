using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Exceptions;
using KPZ.Subscriptions.Domain.Models;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class SubscriptionService(
    ISubscriptionRepository subscriptions,
    IUserRepository users,
    ICategoryRepository categories,
    IPaymentMethodRepository methods,
    IChargeRepository charges,
    ITagRepository tags,
    IValidator<CreateSubscriptionDto> createValidator,
    IValidator<UpdateSubscriptionDto> updateValidator,
    IValidator<SubscriptionQuery> queryValidator) : ISubscriptionService
{
    public async Task<PagedDto<SubscriptionListItemDto>> SearchAsync(SubscriptionQuery q, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(q, ct);
        var filter = new SubscriptionFilter(q.UserId, q.CategoryId, q.Status, q.Search, q.MinAmount,
            q.SortBy, q.Descending, q.Page, q.PageSize);

        return (await subscriptions.SearchAsync(filter, ct)).ToDto(i => i.ToDto());
    }

    public async Task<SubscriptionDetailsDto> GetAsync(int id, CancellationToken ct)
    {
        var s = await subscriptions.GetWithDetailsAsync(id, ct) ?? throw new NotFoundException("Підписку", id);
        return new SubscriptionDetailsDto(
            s.ToDto(),
            s.Charges.OrderBy(c => c.DueDate).Select(c => c.ToDto()).ToList(),
            s.SubscriptionTags.Select(st => new SubscriptionTagDto(st.TagId, st.Tag.Name, st.Note, st.AddedAt)).ToList());
    }

    public async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);

        _ = await users.GetByIdAsync(dto.UserId, ct) ?? throw new NotFoundException("Користувача", dto.UserId);
        _ = await categories.GetByIdAsync(dto.CategoryId, ct) ?? throw new NotFoundException("Категорію", dto.CategoryId);
        await EnsurePaymentMethodOwnedAsync(dto.PaymentMethodId, dto.UserId, ct);

        var id = await subscriptions.AddAsync(new Subscription
        {
            Name = dto.Name.Trim(),
            Notes = dto.Notes,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Period = dto.Period,
            StartDate = dto.StartDate,
            NextChargeDate = dto.StartDate,           // перше списання в день початку
            EndDate = dto.EndDate,
            RemindBeforeDays = dto.RemindBeforeDays,
            Status = SubscriptionStatus.Active,
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            PaymentMethodId = dto.PaymentMethodId
        }, ct);

        // Перше заплановане списання (календар майбутніх платежів)
        await charges.AddAsync(new Charge
        {
            SubscriptionId = id,
            DueDate = dto.StartDate,
            Amount = dto.Amount,
            Status = ChargeStatus.Planned
        }, ct);

        return (await subscriptions.GetByIdAsync(id, ct))!.ToDto();
    }

    public async Task<SubscriptionDto> UpdateAsync(int id, UpdateSubscriptionDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);

        var current = await subscriptions.GetByIdAsync(id, ct) ?? throw new NotFoundException("Підписку", id);

        // Правило переходів: скасовану підписку не можна відновити (створюють нову)
        if (current.Status == SubscriptionStatus.Cancelled && dto.Status != SubscriptionStatus.Cancelled)
            throw new BusinessRuleException("Скасовану підписку не можна відновити.");

        if (dto.CategoryId != current.CategoryId)
            _ = await categories.GetByIdAsync(dto.CategoryId, ct) ?? throw new NotFoundException("Категорію", dto.CategoryId);
        if (dto.PaymentMethodId != current.PaymentMethodId)
            await EnsurePaymentMethodOwnedAsync(dto.PaymentMethodId, current.UserId, ct);

        var endDate = dto.EndDate;
        if (dto.Status == SubscriptionStatus.Cancelled && endDate is null)
            endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var updated = new Subscription
        {
            Id = id,
            Name = dto.Name.Trim(),
            Notes = dto.Notes,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Period = dto.Period,
            StartDate = dto.StartDate,
            NextChargeDate = current.NextChargeDate,   // дату списань змінює лише оплата
            EndDate = endDate,
            RemindBeforeDays = dto.RemindBeforeDays,
            Status = dto.Status,
            UserId = current.UserId,                   // власника змінювати не можна
            CategoryId = dto.CategoryId,
            PaymentMethodId = dto.PaymentMethodId,
            Version = dto.Version                      // версія, яку бачив клієнт
        };

        // false = запису вже немає; ConcurrencyConflictException = Version застаріла
        if (!await subscriptions.UpdateAsync(updated, ct)) throw new NotFoundException("Підписку", id);
        return (await subscriptions.GetByIdAsync(id, ct))!.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await subscriptions.DeleteAsync(id, ct)) throw new NotFoundException("Підписку", id);
    }

    public async Task<IReadOnlyList<SubscriptionListItemDto>> GetUpcomingAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) throw new BusinessRuleException("Дата «до» не може бути раніше за «від».");
        if (to.DayNumber - from.DayNumber > 366) throw new BusinessRuleException("Діапазон не більший за рік.");
        _ = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Користувача", userId);
        return (await subscriptions.GetUpcomingAsync(userId, from, to, ct)).Select(i => i.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<SubscriptionListItemDto>> GetRemindersAsync(DateOnly today, CancellationToken ct)
        => (await subscriptions.GetDueForReminderAsync(today, ct)).Select(i => i.ToDto()).ToList();

    public async Task<IReadOnlyList<CategorySpendingDto>> GetSpendingAsync(int userId, int minCount, CancellationToken ct)
    {
        if (minCount < 0) throw new BusinessRuleException("minCount не може бути від'ємним.");
        _ = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Користувача", userId);

        return (await subscriptions.GetSpendingByCategoryAsync(userId, minCount, ct)).Select(r => r.ToDto()).ToList();
    }

    private async Task EnsurePaymentMethodOwnedAsync(int paymentMethodId, int userId, CancellationToken ct)
    {
        var pm = await methods.GetByIdAsync(paymentMethodId, ct)
                 ?? throw new NotFoundException("Спосіб оплати", paymentMethodId);
        if (pm.UserId != userId)
            throw new BusinessRuleException("Спосіб оплати належить іншому користувачу.");
    }
}
