using FluentValidation;
using KPZ.Subscriptions.BusinessLogic.Dtos;

namespace KPZ.Subscriptions.BusinessLogic.Validators;

public sealed class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class CreatePaymentMethodDtoValidator : AbstractValidator<CreatePaymentMethodDto>
{
    public CreatePaymentMethodDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Last4).Matches(@"^\d{4}$").When(x => x.Last4 is not null)
            .WithMessage("Last4 має містити рівно 4 цифри.");
    }
}

public sealed class UpdatePaymentMethodDtoValidator : AbstractValidator<UpdatePaymentMethodDto>
{
    public UpdatePaymentMethodDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Last4).Matches(@"^\d{4}$").When(x => x.Last4 is not null)
            .WithMessage("Last4 має містити рівно 4 цифри.");
    }
}

public sealed class CreateSubscriptionDtoValidator : AbstractValidator<CreateSubscriptionDto>
{
    public CreateSubscriptionDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.Currency).Matches("^[A-Z]{3}$").WithMessage("Валюта: код з 3 великих літер (UAH, USD).");
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).When(x => x.EndDate is not null)
            .WithMessage("Дата завершення має бути пізніше за дату початку.");
        RuleFor(x => x.RemindBeforeDays).InclusiveBetween(0, 60);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).GreaterThan(0);
    }
}

public sealed class UpdateSubscriptionDtoValidator : AbstractValidator<UpdateSubscriptionDto>
{
    public UpdateSubscriptionDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.Currency).Matches("^[A-Z]{3}$").WithMessage("Валюта: код з 3 великих літер (UAH, USD).");
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).When(x => x.EndDate is not null)
            .WithMessage("Дата завершення має бути пізніше за дату початку.");
        RuleFor(x => x.RemindBeforeDays).InclusiveBetween(0, 60);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).GreaterThan(0);
        RuleFor(x => x.Version).NotNull().Must(v => v is { Length: 8 })
            .WithMessage("Version обов'язкова (8 байт, base64) — значення з відповіді GET.");
    }
}

public sealed class SubscriptionQueryValidator : AbstractValidator<SubscriptionQuery>
{
    private static readonly string[] Sortable = ["name", "amount", "category", "date"];

    public SubscriptionQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinAmount).GreaterThanOrEqualTo(0).When(x => x.MinAmount is not null);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
        RuleFor(x => x.SortBy).Must(s => Sortable.Contains(s!.ToLowerInvariant()))
            .When(x => x.SortBy is not null)
            .WithMessage($"SortBy: {string.Join(", ", Sortable)}.");
    }
}

public sealed class CreateChargeDtoValidator : AbstractValidator<CreateChargeDto>
{
    public CreateChargeDtoValidator()
    {
        RuleFor(x => x.SubscriptionId).GreaterThan(0);
        RuleFor(x => x.DueDate).NotEqual(default(DateOnly));
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, false);
    }
}

public sealed class ChargeQueryValidator : AbstractValidator<ChargeQuery>
{
    public ChargeQueryValidator()
    {
        RuleFor(x => x.SubscriptionId).GreaterThan(0);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}

public sealed class AttachTagDtoValidator : AbstractValidator<AttachTagDto>
{
    public AttachTagDtoValidator() => RuleFor(x => x.Note).MaximumLength(200);
}