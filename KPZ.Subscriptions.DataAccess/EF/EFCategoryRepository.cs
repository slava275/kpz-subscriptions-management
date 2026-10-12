using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.DataAccess.Common;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFCategoryRepository(SubscriptionsDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken ct)
        => db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct)
        => await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<int> AddAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        try 
        { 
            await db.SaveChangesAsync(ct); 
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUnique(ex))
        { 
            throw new DuplicateException($"Категорія «{category.Name}» уже існує.", ex); 
        }
        return category.Id;
    }

    public async Task<bool> UpdateAsync(Category category, CancellationToken ct)
    {
        var entity = await db.Categories.SingleOrDefaultAsync(x => x.Id == category.Id, ct);
        if (entity is null) return false;
        entity.Name = category.Name;
        try 
        { 
            await db.SaveChangesAsync(ct); 
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUnique(ex))
        { 
            throw new DuplicateException($"Категорія «{category.Name}» уже існує.", ex); 
        }
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        try { 
            return await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(ct) > 0;
        }
        catch (Exception ex) when (SqlErrors.IsForeignKey(ex))
        { 
            throw new HasDependenciesException("Категорія містить підписки.", ex); 
        }
    }
}