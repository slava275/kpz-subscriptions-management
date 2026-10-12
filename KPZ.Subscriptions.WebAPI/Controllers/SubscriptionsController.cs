using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Services;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
public sealed class SubscriptionsController(ISubscriptionService service, ITagService tags) : ControllerBase
{
    /// <summary>Пошук підписок: фільтри, сортування, пагінація.</summary>
    [HttpGet]
    [ProducesResponseType<PagedDto<SubscriptionListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDto<SubscriptionListItemDto>>> Search(
        [FromQuery] SubscriptionQuery query, CancellationToken ct)
        => Ok(await service.SearchAsync(query, ct));

    /// <summary>Календар списань користувача за період.</summary>
    [HttpGet("upcoming")]
    [ProducesResponseType<IReadOnlyList<SubscriptionListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionListItemDto>>> Upcoming(
        [FromQuery] int userId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await service.GetUpcomingAsync(userId, from, to, ct));

    /// <summary>Підписки, про списання яких пора нагадати.</summary>
    [HttpGet("reminders")]
    [ProducesResponseType<IReadOnlyList<SubscriptionListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionListItemDto>>> Reminders(CancellationToken ct)
        => Ok(await service.GetRemindersAsync(DateOnly.FromDateTime(DateTime.UtcNow), ct));

    /// <summary>Звіт: витрати активних підписок користувача за категоріями.</summary>
    [HttpGet("spending")]
    [ProducesResponseType<IReadOnlyList<CategorySpendingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategorySpendingDto>>> Spending(
        [FromQuery] int userId, [FromQuery] int minCount = 0, CancellationToken ct = default)
        => Ok(await service.GetSpendingAsync(userId, minCount, ct));

    /// <summary>Повертає підписку зі списаннями та тегами.</summary>
    [HttpGet("{id:int}", Name = "GetSubscription")]
    [ProducesResponseType<SubscriptionDetailsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDetailsDto>> Get(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Створює підписку та перше заплановане списання.</summary>
    /// <response code="422">Спосіб оплати належить іншому користувачу.</response>
    [HttpPost]
    [ProducesResponseType<SubscriptionDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SubscriptionDto>> Create(CreateSubscriptionDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtRoute("GetSubscription", new { id = created.Id }, created);
    }

    /// <summary>Оновлює підписку. Version — значення з останнього GET.</summary>
    /// <response code="409">Підписку змінено паралельно (застаріла Version).</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<SubscriptionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDto>> Update(int id, UpdateSubscriptionDto dto, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, dto, ct));

    /// <summary>Видаляє підписку разом зі списаннями та зв'язками з тегами (Cascade).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Теги підписки.</summary>
    [HttpGet("{id:int}/tags")]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetTags(int id, CancellationToken ct)
        => Ok(await tags.GetBySubscriptionAsync(id, ct));

    /// <summary>Прикріплює тег до підписки (409, якщо вже прикріплено).</summary>
    [HttpPut("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AttachTag(int id, int tagId, AttachTagDto dto, CancellationToken ct)
    {
        await tags.AttachAsync(id, tagId, dto, ct);
        return NoContent();
    }

    /// <summary>Відкріплює тег від підписки.</summary>
    [HttpDelete("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DetachTag(int id, int tagId, CancellationToken ct)
    {
        await tags.DetachAsync(id, tagId, ct);
        return NoContent();
    }
}