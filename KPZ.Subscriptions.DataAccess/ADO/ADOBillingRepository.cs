using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Enums;
using Kpz.Subscriptions.Domain.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Kpz.Subscriptions.Data.Ado;

public sealed class AdoBillingRepository(string connectionString) : IBillingRepository
{
    public async Task<PayChargeResult> PayChargeAsync(int chargeId, DateTime paidAtUtc, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        try
        {
            // 1. Списання (блокуємо рядок до кінця транзакції)
            int subscriptionId; string chargeStatus;
            await using (var cmd = new SqlCommand(
                "SELECT SubscriptionId, Status FROM Charges WITH (UPDLOCK, ROWLOCK) WHERE Id = @id", conn, tx))
            {
                cmd.AddParam("@id", SqlDbType.Int, chargeId);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (!await r.ReadAsync(ct)) return PayChargeResult.ChargeNotFound;
                subscriptionId = r.GetInt32(0);
                chargeStatus = r.GetString(1);
            }
            if (chargeStatus == nameof(ChargeStatus.Paid)) return PayChargeResult.AlreadyPaid;

            // 2. Підписка
            string subStatus, period; DateOnly nextDate; DateOnly? endDate; decimal amount;
            await using (var cmd = new SqlCommand("""
                SELECT Status, Period, NextChargeDate, EndDate, Amount
                FROM Subscriptions WITH (UPDLOCK, ROWLOCK) WHERE Id = @id
                """, conn, tx))
            {
                cmd.AddParam("@id", SqlDbType.Int, subscriptionId);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                await r.ReadAsync(ct);
                subStatus = r.GetString(0);
                period = r.GetString(1);
                nextDate = r.GetDateOnly(2);
                endDate = r.IsDBNull(3) ? null : r.GetDateOnly(3);
                amount = r.GetDecimal(4);
            }
            if (subStatus != nameof(SubscriptionStatus.Active)) return PayChargeResult.SubscriptionNotActive;

            // 3. Таблиця Charges: позначити як оплачене
            await using (var cmd = new SqlCommand(
                "UPDATE Charges SET Status = 'Paid', PaidAt = @paidAt WHERE Id = @id", conn, tx))
            {
                cmd.AddParam("@paidAt", SqlDbType.DateTime2, paidAtUtc);
                cmd.AddParam("@id", SqlDbType.Int, chargeId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 4. Таблиця Subscriptions: посунути дату
            var newNext = Enum.Parse<BillingPeriod>(period)
                .Advance(nextDate);
            await using (var cmd = new SqlCommand(
                "UPDATE Subscriptions SET NextChargeDate = @next WHERE Id = @id", conn, tx))
            {
                cmd.AddParam("@next", SqlDbType.Date, newNext.ToDb());
                cmd.AddParam("@id", SqlDbType.Int, subscriptionId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 5. Таблиця Charges: наступне списання (якщо підписка ще не закінчилась)
            if (endDate is null || newNext <= endDate)
            {
                await using var cmd = new SqlCommand("""
                    INSERT INTO Charges (SubscriptionId, DueDate, Amount, Status)
                    VALUES (@subId, @due, @amount, 'Planned')
                    """, conn, tx);
                cmd.AddParam("@subId", SqlDbType.Int, subscriptionId);
                cmd.AddParam("@due", SqlDbType.Date, newNext.ToDb());
                cmd.AddMoney("@amount", amount);
                await cmd.ExecuteNonQueryAsync(ct);   // CHECK Amount > 0 може викликати відкат
            }

            await tx.CommitAsync(ct);
            return PayChargeResult.Paid;
        }
        catch
        {
            // CancellationToken.None: щоб скасування не завадило відкату
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}