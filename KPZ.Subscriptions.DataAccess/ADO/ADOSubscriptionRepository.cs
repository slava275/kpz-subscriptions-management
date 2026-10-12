using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Exceptions;
using KPZ.Subscriptions.Domain.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Kpz.Subscriptions.Data.Ado;

public sealed class AdoSubscriptionRepository(string connectionString) : ISubscriptionRepository
{
    private const string Columns = """
        s.Id, s.Name, s.Notes, s.Amount, s.Currency, s.Period, s.StartDate, s.NextChargeDate,
        s.EndDate, s.RemindBeforeDays, s.Status, s.UserId, s.CategoryId, s.PaymentMethodId, s.Version
        """;

    private const string ListSelect = """
        SELECT s.Id, s.Name, c.Name AS CategoryName, pm.Title AS PaymentMethodTitle,
               s.Amount, s.Currency, s.Period, s.NextChargeDate, s.Status
        FROM Subscriptions AS s
        INNER JOIN Categories AS c ON c.Id = s.CategoryId
        INNER JOIN PaymentMethods AS pm ON pm.Id = s.PaymentMethodId
        """;


    public async Task<Subscription?> GetByIdAsync(int id, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand($"SELECT {Columns} FROM Subscriptions AS s WHERE s.Id = @id", conn);
        cmd.AddParam("@id", SqlDbType.Int, id);

        await conn.OpenAsync(ct);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    public async Task<Subscription?> GetWithDetailsAsync(int id, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        Subscription? sub;
        await using (var cmd = new SqlCommand($"SELECT {Columns} FROM Subscriptions AS s WHERE s.Id = @id", conn))
        {
            cmd.AddParam("@id", SqlDbType.Int, id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            sub = await r.ReadAsync(ct) ? Map(r) : null;
        }
        if (sub is null) return null;

        await using (var cmd = new SqlCommand(
            "SELECT Id, SubscriptionId, DueDate, Amount, Status, PaidAt FROM Charges WHERE SubscriptionId = @id ORDER BY DueDate", conn))
        {
            cmd.AddParam("@id", SqlDbType.Int, id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                sub.Charges.Add(new Charge
                {
                    Id = r.GetInt32(0),
                    SubscriptionId = r.GetInt32(1),
                    DueDate = r.GetDateOnly(2),
                    Amount = r.GetDecimal(3),
                    Status = Enum.Parse<ChargeStatus>(r.GetString(4)),
                    PaidAt = r.IsDBNull(5) ? null : r.GetDateTime(5)
                });
            }
        }

        await using (var cmd = new SqlCommand("""
            SELECT st.AddedAt, st.Note, t.Id, t.UserId, t.Name
            FROM SubscriptionTags AS st
            INNER JOIN Tags AS t ON t.Id = st.TagId
            WHERE st.SubscriptionId = @id
            """, conn))
        {
            cmd.AddParam("@id", SqlDbType.Int, id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                sub.SubscriptionTags.Add(new SubscriptionTag
                {
                    SubscriptionId = id,
                    TagId = r.GetInt32(2),
                    AddedAt = r.GetDateTime(0),
                    Note = r.IsDBNull(1) ? null : r.GetString(1),
                    Tag = new Tag { Id = r.GetInt32(2), UserId = r.GetInt32(3), Name = r.GetString(4) }
                });
            }
        }
        return sub;
    }

    public async Task<int> AddAsync(Subscription s, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Subscriptions
                (Name, Notes, Amount, Currency, Period, StartDate, NextChargeDate, EndDate,
                 RemindBeforeDays, Status, UserId, CategoryId, PaymentMethodId)
            OUTPUT INSERTED.Id
            VALUES
                (@name, @notes, @amount, @currency, @period, @start, @next, @end,
                 @remind, @status, @userId, @categoryId, @pmId);
            """;

        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        BindWritable(cmd, s);
        cmd.AddParam("@userId", SqlDbType.Int, s.UserId);

        await conn.OpenAsync(ct);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<bool> UpdateAsync(Subscription s, CancellationToken ct)
    {
        const string sql = """
            UPDATE Subscriptions
            SET Name = @name, Notes = @notes, Amount = @amount, Currency = @currency, Period = @period,
                StartDate = @start, NextChargeDate = @next, EndDate = @end, RemindBeforeDays = @remind,
                Status = @status, CategoryId = @categoryId, PaymentMethodId = @pmId
            WHERE Id = @id AND Version = @version;
            """;

        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        BindWritable(cmd, s);
        cmd.AddParam("@id", SqlDbType.Int, s.Id);
        cmd.AddParam("@version", SqlDbType.Binary, s.Version, 8);

        await conn.OpenAsync(ct);
        if (await cmd.ExecuteNonQueryAsync(ct) > 0) return true;

        await using var check = new SqlCommand("SELECT COUNT(1) FROM Subscriptions WHERE Id = @id", conn);
        check.AddParam("@id", SqlDbType.Int, s.Id);
        var exists = Convert.ToInt32(await check.ExecuteScalarAsync(ct)) > 0;

        return exists
            ? throw new ConcurrencyConflictException($"Підписку {s.Id} уже змінено іншим користувачем.")
            : false;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand("DELETE FROM Subscriptions WHERE Id = @id", conn);
        cmd.AddParam("@id", SqlDbType.Int, id);
        await conn.OpenAsync(ct);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }


    public async Task<PagedResult<SubscriptionListItem>> SearchAsync(SubscriptionFilter f, CancellationToken ct)
    {
        var page = Math.Max(f.Page, 1);
        var pageSize = Math.Clamp(f.PageSize, 1, 100);

        var conditions = new List<string>();
        if (f.UserId is not null) conditions.Add("s.UserId = @userId");
        if (f.CategoryId is not null) conditions.Add("s.CategoryId = @categoryId");
        if (f.Status is not null) conditions.Add("s.Status = @status");
        if (!string.IsNullOrWhiteSpace(f.Search)) conditions.Add("s.Name LIKE @search");
        if (f.MinAmount is not null) conditions.Add("s.Amount >= @minAmount");
        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var column = f.SortBy?.ToLowerInvariant() switch
        {
            "name" => "s.Name",
            "amount" => "s.Amount",
            "category" => "c.Name",
            _ => "s.NextChargeDate"
        };
        var order = $"{column} {(f.Descending ? "DESC" : "ASC")}, s.Id";

        void Bind(SqlCommand cmd)
        {
            if (f.UserId is not null) cmd.AddParam("@userId", SqlDbType.Int, f.UserId);
            if (f.CategoryId is not null) cmd.AddParam("@categoryId", SqlDbType.Int, f.CategoryId);
            if (f.Status is not null) cmd.AddParam("@status", SqlDbType.NVarChar, f.Status.ToString(), 20);
            if (!string.IsNullOrWhiteSpace(f.Search)) cmd.AddParam("@search", SqlDbType.NVarChar, $"%{f.Search}%", 200);
            if (f.MinAmount is not null) cmd.AddMoney("@minAmount", f.MinAmount.Value);
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        int total;
        await using (var countCmd = new SqlCommand($"""
            SELECT COUNT(*)
            FROM Subscriptions AS s
            INNER JOIN Categories AS c ON c.Id = s.CategoryId
            INNER JOIN PaymentMethods AS pm ON pm.Id = s.PaymentMethodId
            {where}
            """, conn))
        {
            Bind(countCmd);
            total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
        }

        var items = new List<SubscriptionListItem>();
        await using (var cmd = new SqlCommand($"""
            {ListSelect}
            {where}
            ORDER BY {order}
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """, conn))
        {
            Bind(cmd);
            cmd.AddParam("@skip", SqlDbType.Int, (page - 1) * pageSize);
            cmd.AddParam("@take", SqlDbType.Int, pageSize);
            await ReadList(cmd, items, ct);
        }
        return new PagedResult<SubscriptionListItem>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<SubscriptionListItem>> GetUpcomingAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand($"""
            {ListSelect}
            WHERE s.UserId = @userId AND s.Status = 'Active'
              AND s.NextChargeDate BETWEEN @from AND @to
            ORDER BY s.NextChargeDate, s.Id
            """, conn);
        cmd.AddParam("@userId", SqlDbType.Int, userId);
        cmd.AddParam("@from", SqlDbType.Date, from.ToDb());
        cmd.AddParam("@to", SqlDbType.Date, to.ToDb());

        var items = new List<SubscriptionListItem>();
        await conn.OpenAsync(ct);
        await ReadList(cmd, items, ct);
        return items;
    }

    public async Task<IReadOnlyList<SubscriptionListItem>> GetDueForReminderAsync(DateOnly today, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand($"""
            {ListSelect}
            WHERE s.Status = 'Active'
              AND s.NextChargeDate >= @today
              AND DATEADD(DAY, -s.RemindBeforeDays, s.NextChargeDate) <= @today
            ORDER BY s.NextChargeDate, s.Id
            """, conn);
        cmd.AddParam("@today", SqlDbType.Date, today.ToDb());

        var items = new List<SubscriptionListItem>();
        await conn.OpenAsync(ct);
        await ReadList(cmd, items, ct);
        return items;
    }


    public async Task<IReadOnlyList<CategorySpendingRow>> GetSpendingByCategoryAsync(
        int userId, int minSubscriptionCount, CancellationToken ct)
    {
        const string sql = """
            SELECT c.Id, c.Name,
                   COUNT(s.Id) AS SubscriptionCount,
                   COALESCE(SUM(s.Amount), 0) AS TotalAmount,
                   AVG(s.Amount) AS AverageAmount
            FROM Categories AS c
            LEFT JOIN Subscriptions AS s
                   ON s.CategoryId = c.Id AND s.UserId = @userId AND s.Status = 'Active'
            GROUP BY c.Id, c.Name
            HAVING COUNT(s.Id) >= @minCount
            ORDER BY TotalAmount DESC, c.Name;
            """;

        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.AddParam("@userId", SqlDbType.Int, userId);
        cmd.AddParam("@minCount", SqlDbType.Int, minSubscriptionCount);

        var rows = new List<CategorySpendingRow>();
        await conn.OpenAsync(ct);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            rows.Add(new CategorySpendingRow(
                r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetDecimal(3),
                r.IsDBNull(4) ? null : r.GetDecimal(4)));
        }
        return rows;
    }


    private static void BindWritable(SqlCommand cmd, Subscription s)
    {
        cmd.AddParam("@name", SqlDbType.NVarChar, s.Name, 200);
        cmd.AddParam("@notes", SqlDbType.NVarChar, s.Notes, 1000);
        cmd.AddMoney("@amount", s.Amount);
        cmd.AddParam("@currency", SqlDbType.VarChar, s.Currency, 3);
        cmd.AddParam("@period", SqlDbType.NVarChar, s.Period.ToString(), 20);
        cmd.AddParam("@start", SqlDbType.Date, s.StartDate.ToDb());
        cmd.AddParam("@next", SqlDbType.Date, s.NextChargeDate.ToDb());
        cmd.AddParam("@end", SqlDbType.Date, s.EndDate.ToDb());
        cmd.AddParam("@remind", SqlDbType.Int, s.RemindBeforeDays);
        cmd.AddParam("@status", SqlDbType.NVarChar, s.Status.ToString(), 20);
        cmd.AddParam("@categoryId", SqlDbType.Int, s.CategoryId);
        cmd.AddParam("@pmId", SqlDbType.Int, s.PaymentMethodId);
    }

    private static async Task ReadList(SqlCommand cmd, List<SubscriptionListItem> items, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            items.Add(new SubscriptionListItem(
                r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
                r.GetDecimal(4), r.GetString(5), Enum.Parse<BillingPeriod>(r.GetString(6)),
                r.GetDateOnly(7), Enum.Parse<SubscriptionStatus>(r.GetString(8))));
        }
    }

    private static Subscription Map(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Notes = r.IsDBNull(2) ? null : r.GetString(2),
        Amount = r.GetDecimal(3),
        Currency = r.GetString(4),
        Period = Enum.Parse<BillingPeriod>(r.GetString(5)),
        StartDate = r.GetDateOnly(6),
        NextChargeDate = r.GetDateOnly(7),
        EndDate = r.IsDBNull(8) ? null : r.GetDateOnly(8),
        RemindBeforeDays = r.GetInt32(9),
        Status = Enum.Parse<SubscriptionStatus>(r.GetString(10)),
        UserId = r.GetInt32(11),
        CategoryId = r.GetInt32(12),
        PaymentMethodId = r.GetInt32(13),
        Version = r.GetFieldValue<byte[]>(14)
    };
}