using System.Data;
using Microsoft.Data.SqlClient;
namespace Kpz.Subscriptions.Data.Ado;

internal static class AdoExtensions
{
    public static SqlParameter AddParam(this SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    {
        var p = size > 0 ? cmd.Parameters.Add(name, type, size) : cmd.Parameters.Add(name, type);
        p.Value = value ?? DBNull.Value;
        return p;
    }

    public static SqlParameter AddMoney(this SqlCommand cmd, string name, decimal value)
    {
        var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
        p.Precision = 18; p.Scale = 2; p.Value = value;
        return p;
    }

    public static object ToDb(this DateOnly d) => d.ToDateTime(TimeOnly.MinValue);
    public static object ToDb(this DateOnly? d) => d is null ? DBNull.Value : d.Value.ToDateTime(TimeOnly.MinValue);
    public static DateOnly GetDateOnly(this SqlDataReader r, int i) => DateOnly.FromDateTime(r.GetDateTime(i));
}