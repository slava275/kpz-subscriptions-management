namespace KPZ.Subscriptions.Domain.Exceptions;

public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} з ключем {key} не знайдено.");
