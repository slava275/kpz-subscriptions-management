using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.BusinessLogic.Dtos;

public sealed record PaymentMethodDto(int Id, int UserId, string Title, PaymentMethodType Type, string? Last4);
public sealed record CreatePaymentMethodDto(int UserId, string Title, PaymentMethodType Type, string? Last4);
public sealed record UpdatePaymentMethodDto(string Title, PaymentMethodType Type, string? Last4);
