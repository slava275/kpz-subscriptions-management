using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Exceptions;
using KPZ.Subscriptions.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFSubscriptionRepository(SubscriptionsDbContext db) : ISubscriptionRepository
{
    private static readonly Expression<Func<Subscription, SubscriptionListItem>> ToListItem =
        s => new SubscriptionListItem(
            s.Id, s.Name, s.Category.Name, s.PaymentMethod.Title,
            s.Amount, s.Currency, s.Period, s.NextChargeDate, s.Status);

    public Task<Subscription?> GetByIdAsync(int id, CancellationToken ct)
        => db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);

    public Task<Subscription?> GetWithDetailsAsync(int id, CancellationToken ct)
        => db.Subscriptions.AsNoTracking()
            .Include(s => s.Charges.OrderBy(c => c.DueDate))
            .Include(s => s.SubscriptionTags).ThenInclude(st => st.Tag)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<int> AddAsync(Subscription subscription, CancellationToken ct)
    {
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(ct);
        return subscription.Id;
    }

    public async Task<bool> UpdateAsync(Subscription subscription, CancellationToken ct)
    {
        var entity = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == subscription.Id, ct);
        if (entity is null) return false;

        entity.Name = subscription.Name;
        entity.Notes = subscription.Notes;
        entity.Amount = subscription.Amount;
        entity.Currency = subscription.Currency;
        entity.Period = subscription.Period;
        entity.StartDate = subscription.StartDate;
        entity.NextChargeDate = subscription.NextChargeDate;
        entity.EndDate = subscription.EndDate;
        entity.RemindBeforeDays = subscription.RemindBeforeDays;
        entity.Status = subscription.Status;
        entity.CategoryId = subscription.CategoryId;
        entity.PaymentMethodId = subscription.PaymentMethodId;

        db.Entry(entity).Property(x => x.Version).OriginalValue = subscription.Version;

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException($"Підписку {subscription.Id} уже змінено іншим користувачем.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        => await db.Subscriptions.Where(s => s.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task<PagedResult<SubscriptionListItem>> SearchAsync(SubscriptionFilter filter, CancellationToken ct)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        IQueryable<Subscription> q = db.Subscriptions.AsNoTracking();
        if (filter.UserId is { } userId) q = q.Where(s => s.UserId == userId);
        if (filter.CategoryId is { } categoryId) q = q.Where(s => s.CategoryId == categoryId);
        if (filter.Status is { } status) q = q.Where(s => s.Status == status);
        if (filter.MinAmount is { } min) q = q.Where(s => s.Amount >= min);
        if (!string.IsNullOrWhiteSpace(filter.Search)) q = q.Where(s => s.Name.Contains(filter.Search));

        var total = await q.CountAsync(ct);

        var items = await Sort(q, filter.SortBy, filter.Descending)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToListItem)
            .ToListAsync(ct);

        return new PagedResult<SubscriptionListItem>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<SubscriptionListItem>> GetUpcomingAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken ct)
        => await db.Subscriptions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active
                        && s.NextChargeDate >= from && s.NextChargeDate <= to)
            .OrderBy(s => s.NextChargeDate).ThenBy(s => s.Id)
            .Select(ToListItem)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubscriptionListItem>> GetDueForReminderAsync(DateOnly today, CancellationToken ct)
        => await db.Subscriptions.AsNoTracking()
            .Where(s => s.Status == SubscriptionStatus.Active
                        && s.NextChargeDate >= today
                        && s.NextChargeDate.AddDays(-s.RemindBeforeDays) <= today)
            .OrderBy(s => s.NextChargeDate).ThenBy(s => s.Id)
            .Select(ToListItem)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CategorySpendingRow>> GetSpendingByCategoryAsync(
        int userId, int minSubscriptionCount, CancellationToken ct)
    {
        var rows = await db.Categories.AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                Count = c.Subscriptions.Count(s => s.UserId == userId && s.Status == SubscriptionStatus.Active),
                Total = c.Subscriptions
                    .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                    .Sum(s => s.Amount),
                Avg = c.Subscriptions
                    .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                    .Average(s => (decimal?)s.Amount)
            })
            .Where(x => x.Count >= minSubscriptionCount)
            .OrderByDescending(x => x.Total).ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(x => new CategorySpendingRow(x.Id, x.Name, x.Count, x.Total, x.Avg)).ToList();
    }

    private static IOrderedQueryable<Subscription> Sort(IQueryable<Subscription> q, string? by, bool desc)
        => by?.ToLowerInvariant() switch
        {
            "name" => desc ? q.OrderByDescending(s => s.Name) : q.OrderBy(s => s.Name),
            "amount" => desc ? q.OrderByDescending(s => s.Amount) : q.OrderBy(s => s.Amount),
            "category" => desc ? q.OrderByDescending(s => s.Category.Name) : q.OrderBy(s => s.Category.Name),
            _ => desc ? q.OrderByDescending(s => s.NextChargeDate) : q.OrderBy(s => s.NextChargeDate)
        };
}