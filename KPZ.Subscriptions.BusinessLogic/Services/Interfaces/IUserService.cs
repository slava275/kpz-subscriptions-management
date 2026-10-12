using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct);
    Task<UserDto> GetAsync(int id, CancellationToken ct);
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct);
    Task<UserDto> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
