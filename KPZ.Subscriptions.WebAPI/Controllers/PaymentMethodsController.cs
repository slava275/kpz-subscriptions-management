using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Services;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Controllers;

[ApiController]
[Route("api/v1/payment-methods")]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class PaymentMethodsController(IPaymentMethodService service) : ControllerBase
{
    /// <summary>Повертає способи оплати користувача.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PaymentMethodDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodDto>>> GetByUser(
        [FromQuery] int userId, CancellationToken ct)
        => Ok(await service.GetByUserAsync(userId, ct));

    /// <summary>Повертає спосіб оплати за ідентифікатором.</summary>
    [HttpGet("{id:int}", Name = "GetPaymentMethod")]
    [ProducesResponseType<PaymentMethodDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMethodDto>> Get(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Створює спосіб оплати.</summary>
    [HttpPost]
    [ProducesResponseType<PaymentMethodDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentMethodDto>> Create(CreatePaymentMethodDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtRoute("GetPaymentMethod", new { id = created.Id }, created);
    }

    /// <summary>Оновлює спосіб оплати.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<PaymentMethodDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMethodDto>> Update(int id, UpdatePaymentMethodDto dto, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, dto, ct));

    /// <summary>Видаляє спосіб оплати (409, якщо його використовують підписки).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}