using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface IChargeService
{
    Task<PagedDto<ChargeDto>> GetPagedAsync(ChargeQuery query, CancellationToken ct);
    Task<ChargeDto> GetAsync(int id, CancellationToken ct);
    Task<ChargeDto> CreateAsync(CreateChargeDto dto, CancellationToken ct);
    Task<ChargeDto> PayAsync(int id, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
