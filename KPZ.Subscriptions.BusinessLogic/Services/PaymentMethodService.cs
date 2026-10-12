using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class PaymentMethodService(
    IPaymentMethodRepository methods,
    IUserRepository users,
    IValidator<CreatePaymentMethodDto> createValidator,
    IValidator<UpdatePaymentMethodDto> updateValidator) : IPaymentMethodService
{
    public async Task<IReadOnlyList<PaymentMethodDto>> GetByUserAsync(int userId, CancellationToken ct)
    {
        _ = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Користувача", userId);
        return (await methods.GetByUserAsync(userId, ct)).Select(p => p.ToDto()).ToList();
    }

    public async Task<PaymentMethodDto> GetAsync(int id, CancellationToken ct)
        => (await methods.GetByIdAsync(id, ct) ?? throw new NotFoundException("Спосіб оплати", id)).ToDto();

    public async Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        _ = await users.GetByIdAsync(dto.UserId, ct) ?? throw new NotFoundException("Користувача", dto.UserId);

        var id = await methods.AddAsync(new PaymentMethod
        {
            UserId = dto.UserId,
            Title = dto.Title.Trim(),
            Type = dto.Type,
            Last4 = dto.Last4
        }, ct);
        return await GetAsync(id, ct);
    }

    public async Task<PaymentMethodDto> UpdateAsync(int id, UpdatePaymentMethodDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);
        var ok = await methods.UpdateAsync(new PaymentMethod
        {
            Id = id,
            Title = dto.Title.Trim(),
            Type = dto.Type,
            Last4 = dto.Last4
        }, ct);
        if (!ok) throw new NotFoundException("Спосіб оплати", id);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await methods.DeleteAsync(id, ct)) throw new NotFoundException("Спосіб оплати", id);
    }
}