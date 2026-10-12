namespace KPZ.Subscriptions.Domain.Exceptions;

public sealed class ConcurrencyConflictException(string message, Exception? inner = null) 
    : Exception(message, inner);
