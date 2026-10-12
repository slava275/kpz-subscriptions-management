namespace KPZ.Subscriptions.Domain.Exceptions;

public sealed class HasDependenciesException(string message, Exception? inner = null) 
    : Exception(message, inner);
