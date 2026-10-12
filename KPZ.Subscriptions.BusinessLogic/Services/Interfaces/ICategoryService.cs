using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct);
    Task<CategoryDto> GetAsync(int id, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
