using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface IPaymentMethodService
{
    Task<IReadOnlyList<PaymentMethodDto>> GetByUserAsync(int userId, CancellationToken ct);
    Task<PaymentMethodDto> GetAsync(int id, CancellationToken ct);
    Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto, CancellationToken ct);
    Task<PaymentMethodDto> UpdateAsync(int id, UpdatePaymentMethodDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
