using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class UserService(
    IUserRepository users,
    IValidator<CreateUserDto> createValidator,
    IValidator<UpdateUserDto> updateValidator) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct)
        => (await users.GetAllAsync(ct)).Select(u => u.ToDto()).ToList();

    public async Task<UserDto> GetAsync(int id, CancellationToken ct)
        => (await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("Користувача не знайдено.", id)).ToDto();

    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        var email = dto.Email.Trim().ToLowerInvariant();

        // Зрозуміле повідомлення до збереження; остаточну гарантію дає unique-індекс
        if (await users.EmailExistsAsync(email, ct))
            throw new DuplicateException($"Email {email} уже зайнятий.");

        var id = await users.AddAsync(
            new User { Name = dto.Name.Trim(), Email = email, CreatedAt = DateTime.UtcNow }, ct);
        return await GetAsync(id, ct);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);
        var email = dto.Email.Trim().ToLowerInvariant();

        _ = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("Користувача", id);

        var other = await users.GetByEmailAsync(email, ct);
        if (other is not null && other.Id != id)
            throw new DuplicateException($"Email {email} уже зайнятий.");

        if (!await users.UpdateAsync(new User { Id = id, Name = dto.Name.Trim(), Email = email }, ct))
            throw new NotFoundException("Користувача", id);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await users.DeleteAsync(id, ct)) throw new NotFoundException("Користувача", id);
    }
}
