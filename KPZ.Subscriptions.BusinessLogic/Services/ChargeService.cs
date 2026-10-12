using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Exceptions;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class ChargeService(
    IChargeRepository charges,
    ISubscriptionRepository subscriptions,
    IBillingRepository billing,
    IValidator<CreateChargeDto> createValidator,
    IValidator<ChargeQuery> queryValidator) : IChargeService
{
    public async Task<PagedDto<ChargeDto>> GetPagedAsync(ChargeQuery q, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(q, ct);
        _ = await subscriptions.GetByIdAsync(q.SubscriptionId, ct) ?? throw new NotFoundException("Підписку", q.SubscriptionId);
        return (await charges.GetPagedAsync(q.SubscriptionId, q.Status, q.Page, q.PageSize, ct)).ToDto(c => c.ToDto());
    }

    public async Task<ChargeDto> GetAsync(int id, CancellationToken ct)
        => (await charges.GetByIdAsync(id, ct) ?? throw new NotFoundException("Списання", id)).ToDto();

    public async Task<ChargeDto> CreateAsync(CreateChargeDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        _ = await subscriptions.GetByIdAsync(dto.SubscriptionId, ct) ?? throw new NotFoundException("Підписку", dto.SubscriptionId);

        var id = await charges.AddAsync(new Charge
        {
            SubscriptionId = dto.SubscriptionId,
            DueDate = dto.DueDate,
            Amount = dto.Amount,
            Status = ChargeStatus.Planned
        }, ct);
        return await GetAsync(id, ct);
    }

    // Транзакційна операція: Charge -> Paid, зсув NextChargeDate, наступний Charge
    public async Task<ChargeDto> PayAsync(int id, CancellationToken ct)
    {
        var result = await billing.PayChargeAsync(id, DateTime.UtcNow, ct);
        return result switch
        {
            PayChargeResult.Paid => await GetAsync(id, ct),
            PayChargeResult.ChargeNotFound => throw new NotFoundException("Списання", id),
            PayChargeResult.AlreadyPaid => throw new DuplicateException("Списання вже оплачено."),
            PayChargeResult.SubscriptionNotActive => throw new BusinessRuleException("Підписка не активна."),
            _ => throw new InvalidOperationException($"Невідомий результат {result}.")
        };
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await charges.DeleteAsync(id, ct)) throw new NotFoundException("Списання", id);
    }
}
