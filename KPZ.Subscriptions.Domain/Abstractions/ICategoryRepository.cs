using KPZ.Subscriptions.Domain.Entities;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);
    Task<int> AddAsync(Category category, CancellationToken ct);
    Task<bool> UpdateAsync(Category category, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
