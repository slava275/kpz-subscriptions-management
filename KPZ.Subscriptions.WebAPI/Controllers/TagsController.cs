using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Services;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Controllers;

[ApiController]
[Route("api/v1/tags")]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class TagsController(ITagService service) : ControllerBase
{
    /// <summary>Теги користувача.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetByUser([FromQuery] int userId, CancellationToken ct)
        => Ok(await service.GetByUserAsync(userId, ct));

    /// <summary>Створює тег (409, якщо в користувача такий уже є).</summary>
    [HttpPost]
    [ProducesResponseType<TagDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TagDto>> Create(CreateTagDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return Created($"/api/v1/tags/{created.Id}", created);
    }

    /// <summary>Видаляє тег разом зі зв'язками з підписками (Cascade).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}