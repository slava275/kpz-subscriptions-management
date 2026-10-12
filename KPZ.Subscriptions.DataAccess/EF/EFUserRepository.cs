using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.DataAccess.Common;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFUserRepository(SubscriptionsDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken ct)
        => db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        => db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct)
        => await db.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync(ct);

    public async Task<int> AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        try 
        { 
            await db.SaveChangesAsync(ct); 
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUnique(ex))
        {
            db.Entry(user).State = EntityState.Detached;
            throw new DuplicateException($"Email {user.Email} уже зайнятий.", ex); 
        }
        return user.Id;
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken ct)
    {
        var entity = await db.Users.SingleOrDefaultAsync(u => u.Id == user.Id, ct);
        if (entity is null) return false;
        entity.Name = user.Name;
        entity.Email = user.Email;

        try { 
            await db.SaveChangesAsync(ct); 
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUnique(ex))
        { 
            throw new DuplicateException($"Email {user.Email} уже зайнятий.", ex); 
        }
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        try { 
            return await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct) > 0; 
        }
        catch (Exception ex) when (SqlErrors.IsForeignKey(ex))
        { 
            throw new HasDependenciesException("У користувача є підписки, способи оплати або теги.", ex); 
        }
    }
}