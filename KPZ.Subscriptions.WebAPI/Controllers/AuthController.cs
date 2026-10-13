using System.Security.Claims;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth, IJwtTokenService jwt) : ControllerBase
{
    public const string ExternalScheme = "External";
    private static readonly string[] Providers = ["Google"];

    public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAt, UserDto User);

    /// <summary>
    /// Починає вхід через зовнішнього провайдера (редірект на сторінку Google).
    /// Відкривати в браузері, а не через кнопку Execute у Swagger.
    /// </summary>
    [HttpGet("login/{provider}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Login(string provider)
    {
        var name = Providers.FirstOrDefault(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (name is null) return NotFound(new ProblemDetails { Title = "Невідомий провайдер", Status = 404 });

        return Challenge(new AuthenticationProperties { RedirectUri = "/api/v1/auth/callback" }, name);
    }

    /// <summary>Повернення від провайдера: створює або знаходить користувача та видає JWT.</summary>
    [HttpGet("callback")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TokenResponse>> Callback(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(ExternalScheme);
        if (!result.Succeeded || result.Principal is null)
            return Unauthorized(new ProblemDetails { Title = "Зовнішній вхід не виконано", Status = 401 });

        var p = result.Principal;
        var provider = result.Properties?.Items[".AuthScheme"] ?? "Google";
        var identity = new ExternalIdentityDto(
            Provider: provider,
            Subject: p.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
            Email: p.FindFirstValue(ClaimTypes.Email) ?? "",
            EmailVerified: string.Equals(p.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase),
            Name: p.FindFirstValue(ClaimTypes.Name),
            AvatarUrl: p.FindFirstValue("urn:google:picture"));

        // Тимчасовий cookie більше не потрібен
        await HttpContext.SignOutAsync(ExternalScheme);

        var user = await auth.SignInExternalAsync(identity, ct);
        var (token, expires) = jwt.Create(user);
        return Ok(new TokenResponse(token, "Bearer", expires, user));
    }

    /// <summary>Поточний користувач за даними з JWT.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue("sub"),
        Email = User.FindFirstValue("email"),
        Name = User.FindFirstValue("name"),
        Role = User.FindFirstValue("role")
    });
}