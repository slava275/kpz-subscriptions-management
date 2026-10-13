using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class AuthService(IUserRepository users) : IAuthService
{
    public async Task<UserDto> SignInExternalAsync(ExternalIdentityDto id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id.Subject) || string.IsNullOrWhiteSpace(id.Email))
            throw new BusinessRuleException("Провайдер не повернув ідентифікатор або email.");

        var email = id.Email.Trim().ToLowerInvariant();

        // 1. Ключ входу — пара (provider, subject), а не email
        var user = await users.GetByExternalLoginAsync(id.Provider, id.Subject, ct);

        if (user is null)
        {
            var byEmail = await users.GetByEmailAsync(email, ct);
            if (byEmail is not null)
            {
                // Прив'язувати акаунт за email можна лише якщо провайдер підтвердив email,
                // інакше можливе захоплення чужого облікового запису
                if (!id.EmailVerified)
                    throw new BusinessRuleException(
                        "Користувач із таким email уже існує, а провайдер не підтвердив email.");
                user = byEmail;
            }
            else
            {
                var name = string.IsNullOrWhiteSpace(id.Name) ? email.Split('@')[0] : id.Name.Trim();
                var newId = await users.AddAsync(new User
                {
                    Name = name.Length > 100 ? name[..100] : name,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    AvatarUrl = id.AvatarUrl
                }, ct);
                user = (await users.GetByIdAsync(newId, ct))!;
            }

            await users.AddExternalLoginAsync(user.Id, new ExternalLogin
            {
                Provider = id.Provider,
                ProviderSubject = id.Subject,
                ProviderEmail = email,
                LinkedAt = DateTime.UtcNow
            }, ct);
        }

        if (!user.IsActive) throw new ForbiddenException("Обліковий запис заблоковано.");

        await users.RecordLoginAsync(user.Id, DateTime.UtcNow, id.AvatarUrl, ct);
        return (await users.GetByIdAsync(user.Id, ct))!.ToDto();
    }
}