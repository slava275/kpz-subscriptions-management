using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Знаходить користувача за зовнішнім акаунтом або створює нового.</summary>
    Task<UserDto> SignInExternalAsync(ExternalIdentityDto identity, CancellationToken ct);
}
