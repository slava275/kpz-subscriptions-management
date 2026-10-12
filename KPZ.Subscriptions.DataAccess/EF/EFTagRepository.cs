using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.DataAccess.Common;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFTagRepository(SubscriptionsDbContext db) : ITagRepository
{
    public Task<Tag?> GetByIdAsync(int id, CancellationToken ct)
        => db.Tags.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Tag>> GetByUserAsync(int userId, CancellationToken ct)
        => await db.Tags.AsNoTracking().Where(t => t.UserId == userId).OrderBy(t => t.Name).ToListAsync(ct);

    public Task<bool> ExistsAsync(int userId, string name, CancellationToken ct)
        => db.Tags.AnyAsync(t => t.UserId == userId && t.Name == name, ct);

    public async Task<int> AddAsync(Tag tag, CancellationToken ct)
    {
        db.Tags.Add(tag);
        try 
        { 
            await db.SaveChangesAsync(ct); 
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUnique(ex))
        {
            db.Entry(tag).State = EntityState.Detached;
            throw new DuplicateException($"Тег «{tag.Name}» уже існує.", ex); 
        }
        return tag.Id;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        => await db.Tags.Where(t => t.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task AttachAsync(int subscriptionId, int tagId, string? note, CancellationToken ct)
    {
        var link = new SubscriptionTag
        {
            SubscriptionId = subscriptionId,
            TagId = tagId,
            Note = note,
            AddedAt = DateTime.UtcNow
        };

        db.SubscriptionTags.Add(link);
        try 
        { 
            await db.SaveChangesAsync(ct); 
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUnique(ex))
        {
            db.Entry(link).State = EntityState.Detached;
            throw new DuplicateException("Тег уже прикріплено до цієї підписки.", ex); 
        }
    }

    public async Task<bool> DetachAsync(int subscriptionId, int tagId, CancellationToken ct)
        => await db.SubscriptionTags
            .Where(st => st.SubscriptionId == subscriptionId && st.TagId == tagId)
            .ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyList<Tag>> GetBySubscriptionAsync(int subscriptionId, CancellationToken ct)
        => await db.SubscriptionTags.AsNoTracking()
            .Where(st => st.SubscriptionId == subscriptionId)
            .Select(st => st.Tag)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
}