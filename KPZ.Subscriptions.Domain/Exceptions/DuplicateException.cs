namespace KPZ.Subscriptions.Domain.Exceptions;

public sealed class DuplicateException(string message, Exception? inner = null) 
    : Exception(message, inner);
