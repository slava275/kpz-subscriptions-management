using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.DataAccess.Common;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFPaymentMethodRepository(SubscriptionsDbContext db) : IPaymentMethodRepository
{
    public Task<PaymentMethod?> GetByIdAsync(int id, CancellationToken ct)
        => db.PaymentMethods.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PaymentMethod>> GetByUserAsync(int userId, CancellationToken ct)
        => await db.PaymentMethods.AsNoTracking()
            .Where(p => p.UserId == userId).OrderBy(p => p.Title).ToListAsync(ct);

    public async Task<int> AddAsync(PaymentMethod method, CancellationToken ct)
    {
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync(ct);
        return method.Id;
    }

    public async Task<bool> UpdateAsync(PaymentMethod method, CancellationToken ct)
    {
        var entity = await db.PaymentMethods.SingleOrDefaultAsync(p => p.Id == method.Id, ct);
        if (entity is null) return false;

        entity.Title = method.Title;
        entity.Type = method.Type;
        entity.Last4 = method.Last4;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        try 
        { 
            return await db.PaymentMethods.Where(p => p.Id == id).ExecuteDeleteAsync(ct) > 0; 
        }
        catch (Exception ex) when (SqlErrors.IsForeignKey(ex))
        { 
            throw new HasDependenciesException("Спосіб оплати використовується в підписках.", ex); 
        }
    }
}