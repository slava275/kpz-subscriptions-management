using KPZ.Subscriptions.BusinessLogic.Dtos;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Models;

namespace KPZ.Subscriptions.BusinessLogic.Mapping;

public static class Mapping
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt, u.AvatarUrl);

    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name);

    public static PaymentMethodDto ToDto(this PaymentMethod p) =>
        new(p.Id, p.UserId, p.Title, p.Type, p.Last4);

    public static SubscriptionDto ToDto(this Subscription s) =>
        new(s.Id, s.Name, s.Notes, s.Amount, s.Currency, s.Period, s.StartDate, s.NextChargeDate,
            s.EndDate, s.RemindBeforeDays, s.Status, s.UserId, s.CategoryId, s.PaymentMethodId, s.Version);

    public static SubscriptionListItemDto ToDto(this SubscriptionListItem i) =>
        new(i.Id, i.Name, i.CategoryName, i.PaymentMethodTitle, i.Amount, i.Currency, i.Period,
            i.NextChargeDate, i.Status);

    public static ChargeDto ToDto(this Charge c) =>
        new(c.Id, c.SubscriptionId, c.DueDate, c.Amount, c.Status, c.PaidAt);

    public static TagDto ToDto(this Tag t) => new(t.Id, t.UserId, t.Name);

    public static CategorySpendingDto ToDto(this CategorySpendingRow r) =>
        new(r.CategoryId, r.CategoryName, r.SubscriptionCount, r.TotalAmount, r.AverageAmount);

    public static PagedDto<TOut> ToDto<TIn, TOut>(this PagedResult<TIn> p, Func<TIn, TOut> map) =>
        new(p.Items.Select(map).ToList(), p.TotalCount, p.Page, p.PageSize);
}