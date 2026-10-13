namespace KPZ.Subscriptions.Domain.Exceptions;

public sealed class BusinessRuleException(string message) : Exception(message);
