using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Services;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Controllers;

[ApiController]
[Route("api/v1/charges")]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
public sealed class ChargesController(IChargeService service) : ControllerBase
{
    /// <summary>Історія списань підписки з пагінацією (subscriptionId обов'язковий).</summary>
    [HttpGet]
    [ProducesResponseType<PagedDto<ChargeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDto<ChargeDto>>> GetPaged([FromQuery] ChargeQuery query, CancellationToken ct)
        => Ok(await service.GetPagedAsync(query, ct));

    /// <summary>Повертає списання за ідентифікатором.</summary>
    [HttpGet("{id:int}", Name = "GetCharge")]
    [ProducesResponseType<ChargeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ChargeDto>> Get(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Створює заплановане списання.</summary>
    [HttpPost]
    [ProducesResponseType<ChargeDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ChargeDto>> Create(CreateChargeDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtRoute("GetCharge", new { id = created.Id }, created);
    }

    /// <summary>
    /// Оплачує списання: у транзакції позначає Paid, зсуває дату наступного списання
    /// та створює наступне заплановане списання.
    /// </summary>
    /// <response code="409">Списання вже оплачено.</response>
    /// <response code="422">Підписка не активна.</response>
    [HttpPost("{id:int}/pay")]
    [ProducesResponseType<ChargeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ChargeDto>> Pay(int id, CancellationToken ct)
        => Ok(await service.PayAsync(id, ct));

    /// <summary>Видаляє списання.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}