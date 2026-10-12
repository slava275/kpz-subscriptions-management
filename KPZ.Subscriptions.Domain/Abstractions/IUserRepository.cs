using KPZ.Subscriptions.Domain.Entities;

namespace KPZ.Subscriptions.Domain.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct);
    Task<User?> GetByExternalLoginAsync(string provider, string subject, CancellationToken ct);
    Task AddExternalLoginAsync(int userId, ExternalLogin login, CancellationToken ct);
    Task<int> AddAsync(User user, CancellationToken ct);
    Task<bool> UpdateAsync(User user, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
