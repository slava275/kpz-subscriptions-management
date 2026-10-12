using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using Kpz.Subscriptions.Domain.Models;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Subscriptions.DataAccess.EF;

public sealed class EFBillingRepository(SubscriptionsDbContext db) : IBillingRepository
{
    public async Task<PayChargeResult> PayChargeAsync(int chargeId, DateTime paidAtUtc, CancellationToken ct)
    {
        // EnableRetryOnFailure несумісний з ручними транзакціями, тому вся одиниця роботи в стратегії
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();   // повтор стратегії починається з чистого стану
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var charge = await db.Charges
                    .Include(c => c.Subscription)
                    .SingleOrDefaultAsync(c => c.Id == chargeId, ct);

                if (charge is null) return PayChargeResult.ChargeNotFound;
                if (charge.Status == ChargeStatus.Paid) return PayChargeResult.AlreadyPaid;

                var sub = charge.Subscription;
                if (sub.Status != SubscriptionStatus.Active) return PayChargeResult.SubscriptionNotActive;

                charge.Status = ChargeStatus.Paid;
                charge.PaidAt = paidAtUtc;

                sub.NextChargeDate = sub.Period.Advance(sub.NextChargeDate);

                if (sub.EndDate is null || sub.NextChargeDate <= sub.EndDate)
                {
                    db.Charges.Add(new Charge
                    {
                        SubscriptionId = sub.Id,
                        DueDate = sub.NextChargeDate,
                        Amount = sub.Amount,
                        Status = ChargeStatus.Planned
                    });
                }

                await db.SaveChangesAsync(ct);   // UPDATE Charges, UPDATE Subscriptions (з перевіркою Version), INSERT Charges
                await tx.CommitAsync(ct);
                return PayChargeResult.Paid;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(CancellationToken.None);
                if (ex is DbUpdateConcurrencyException)
                    throw new ConcurrencyConflictException("Підписку змінено паралельно, повторіть операцію.", ex);
                throw;
            }
        });
    }
}