using KPZ.Subscriptions.DataAccess.Common;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Kpz.Subscriptions.Data.Ado;

public sealed class AdoCategoryRepository(string connectionString) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand("SELECT Id, Name FROM Categories WHERE Id = @id", conn);

        cmd.AddParam("@id", SqlDbType.Int, id);
        await conn.OpenAsync(ct);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new Category { Id = r.GetInt32(0), Name = r.GetString(1) } : null;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand("SELECT Id, Name FROM Categories ORDER BY Name", conn);
        var list = new List<Category>();
        await conn.OpenAsync(ct);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new Category { Id = r.GetInt32(0), Name = r.GetString(1) });
        return list;
    }

    public async Task<int> AddAsync(Category c, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand("INSERT INTO Categories(Name) OUTPUT INSERTED.Id VALUES (@name)", conn);
        cmd.AddParam("@name", SqlDbType.NVarChar, c.Name, 100);
        await conn.OpenAsync(ct);
        try { return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)); }
        catch (SqlException ex) when (SqlErrors.IsUnique(ex))
        { throw new DuplicateException($"Категорія «{c.Name}» уже існує.", ex); }
    }

    public async Task<bool> UpdateAsync(Category c, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand("UPDATE Categories SET Name = @name WHERE Id = @id", conn);
        cmd.AddParam("@name", SqlDbType.NVarChar, c.Name, 100);
        cmd.AddParam("@id", SqlDbType.Int, c.Id);
        await conn.OpenAsync(ct);
        try { return await cmd.ExecuteNonQueryAsync(ct) > 0; }
        catch (SqlException ex) when (SqlErrors.IsUnique(ex))
        { throw new DuplicateException($"Категорія «{c.Name}» уже існує.", ex); }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand("DELETE FROM Categories WHERE Id = @id", conn);
        cmd.AddParam("@id", SqlDbType.Int, id);
        await conn.OpenAsync(ct);
        try { return await cmd.ExecuteNonQueryAsync(ct) > 0; }
        catch (SqlException ex) when (SqlErrors.IsForeignKey(ex))
        { throw new HasDependenciesException("Категорія містить підписки.", ex); }
    }
}