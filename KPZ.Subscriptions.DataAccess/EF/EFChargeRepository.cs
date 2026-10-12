using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFChargeRepository(SubscriptionsDbContext db) : IChargeRepository
{
    public Task<Charge?> GetByIdAsync(int id, CancellationToken ct)
        => db.Charges.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Charge>> GetBySubscriptionAsync(int subscriptionId, CancellationToken ct)
        => await db.Charges.AsNoTracking()
            .Where(c => c.SubscriptionId == subscriptionId)
            .OrderBy(c => c.DueDate).ThenBy(c => c.Id)
            .ToListAsync(ct);

    public async Task<PagedResult<Charge>> GetPagedAsync(
        int subscriptionId, ChargeStatus? status, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Charge> q = db.Charges.AsNoTracking().Where(c => c.SubscriptionId == subscriptionId);
        if (status is { } s) q = q.Where(c => c.Status == s);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.DueDate).ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Charge>(items, total, page, pageSize);
    }

    public async Task<int> AddAsync(Charge charge, CancellationToken ct)
    {
        db.Charges.Add(charge);
        await db.SaveChangesAsync(ct);
        return charge.Id;
    }

    public async Task<bool> UpdateAsync(Charge charge, CancellationToken ct)
    {
        var entity = await db.Charges.SingleOrDefaultAsync(c => c.Id == charge.Id, ct);
        if (entity is null) return false;

        entity.DueDate = charge.DueDate;
        entity.Amount = charge.Amount;
        entity.Status = charge.Status;
        entity.PaidAt = charge.PaidAt;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        => await db.Charges.Where(c => c.Id == id).ExecuteDeleteAsync(ct) > 0;
}