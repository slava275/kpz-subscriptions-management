using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class UsersController(IUserService service) : ControllerBase
{
    /// <summary>Повертає список користувачів.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    /// <summary>Повертає користувача за ідентифікатором.</summary>
    [HttpGet("{id:int}", Name = "GetUser")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Створює користувача.</summary>
    /// <response code="201">Створено; заголовок Location вказує на ресурс.</response>
    /// <response code="409">Email уже зайнятий.</response>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtRoute("GetUser", new { id = created.Id }, created);
    }

    /// <summary>Оновлює ім'я та email (PUT ідемпотентний).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserDto dto, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, dto, ct));

    /// <summary>Видаляє користувача.</summary>
    /// <response code="409">У користувача є підписки, способи оплати або теги (Restrict).</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}