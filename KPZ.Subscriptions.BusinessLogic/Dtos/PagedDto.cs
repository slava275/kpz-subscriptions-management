namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record PagedDto<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}