using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.BusinessLogic.Mapping;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;

namespace KPZ.Subscriptions.BusinessLogic.Services;

public sealed class CategoryService(
    ICategoryRepository categories,
    IValidator<CreateCategoryDto> createValidator,
    IValidator<UpdateCategoryDto> updateValidator) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct)
        => (await categories.GetAllAsync(ct)).Select(c => c.ToDto()).ToList();

    public async Task<CategoryDto> GetAsync(int id, CancellationToken ct)
        => (await categories.GetByIdAsync(id, ct) ?? throw new NotFoundException("Категорію", id)).ToDto();

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        var id = await categories.AddAsync(new Category { Name = dto.Name.Trim() }, ct);
        return await GetAsync(id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);
        if (!await categories.UpdateAsync(new Category { Id = id, Name = dto.Name.Trim() }, ct))
            throw new NotFoundException("Категорію", id);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await categories.DeleteAsync(id, ct)) throw new NotFoundException("Категорію", id);
    }
}
