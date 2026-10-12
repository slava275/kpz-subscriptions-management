using KPZ.Subscriptions.Domain.Entities;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface IPaymentMethodRepository
{
    Task<PaymentMethod?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<PaymentMethod>> GetByUserAsync(int userId, CancellationToken ct);
    Task<int> AddAsync(PaymentMethod method, CancellationToken ct);
    Task<bool> UpdateAsync(PaymentMethod method, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
