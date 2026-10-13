using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.WebAPI.Infrastructure;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) Create(UserDto user);
}
