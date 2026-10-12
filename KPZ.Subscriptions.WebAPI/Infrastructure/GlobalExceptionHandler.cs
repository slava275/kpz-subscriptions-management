using FluentValidation;
using KPZ.Subscriptions.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KPZ.Subscriptions.WebAPI.Infrastructure;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment env,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException ve:
                problem = new ValidationProblemDetails(ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                { Status = StatusCodes.Status400BadRequest, Title = "Помилка валідації" };
                break;
            case NotFoundException:
                problem = Make(StatusCodes.Status404NotFound, "Ресурс не знайдено", exception.Message); break;
            case DuplicateException:
                problem = Make(StatusCodes.Status409Conflict, "Конфлікт: дублікат", exception.Message); break;
            case HasDependenciesException:
                problem = Make(StatusCodes.Status409Conflict, "Конфлікт: є залежні дані", exception.Message); break;
            case ConcurrencyConflictException:
                problem = Make(StatusCodes.Status409Conflict, "Конфлікт версій",
                    exception.Message + " Отримайте актуальну версію й повторіть запит."); break;
            case BusinessRuleException:
                problem = Make(StatusCodes.Status422UnprocessableEntity, "Порушено бізнес-правило", exception.Message); break;
            case OperationCanceledException:
                return false;
            default:
                logger.LogError(exception, "Необроблений виняток");
                problem = Make(StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера",
                    env.IsDevelopment() ? exception.ToString() : null);
                break;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Make(int status, string title, string? detail)
        => new() { Status = status, Title = title, Detail = detail };
}