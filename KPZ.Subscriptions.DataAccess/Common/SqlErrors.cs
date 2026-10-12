using Microsoft.Data.SqlClient;

namespace KPZ.Subscriptions.DataAccess.Common;

internal static class SqlErrors
{
    public static bool IsUnique(Exception ex) => Has(ex, 2627, 2601);
    public static bool IsForeignKey(Exception ex) => Has(ex, 547);

    private static bool Has(Exception ex, params int[] numbers)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is SqlException sql && numbers.Contains(sql.Number)) return true;
        return false;
    }
}
