using FluentValidation;
using Kpz.Subscriptions.Data.Ado;
using Kpz.Subscriptions.DataAccess.EF;
using KPZ.Subscriptions.BusinessLogic.Services;
using KPZ.Subscriptions.BusinessLogic.Services.Interfaces;
using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.DataAccess.Seeding;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.WebAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KPZ.Subscriptions.WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connectionString = builder.Configuration.GetConnectionString("KpzDb")
            ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

        builder.Services.AddDbContext<SubscriptionsDbContext>(options => options
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(SubscriptionsDbContext).Assembly.GetName().Name);
                sql.EnableRetryOnFailure(3);
            })
            .UseSeeding((context, _) =>
            {
                if (!context.Set<User>().Any())
                {
                    context.AddRange(SeedData.BuildUsers());
                    context.SaveChanges();
                }
            })
            .UseAsyncSeeding(async (context, _, ct) =>
            {
                if (!await context.Set<User>().AnyAsync(ct))
                {
                    context.AddRange(SeedData.BuildUsers());
                    await context.SaveChangesAsync(ct);
                }
            }));

        // Тільки EF
        builder.Services.AddScoped<IUserRepository, EFUserRepository>();
        builder.Services.AddScoped<IPaymentMethodRepository, EFPaymentMethodRepository>();
        builder.Services.AddScoped<IChargeRepository, EFChargeRepository>();
        builder.Services.AddScoped<ITagRepository, EFTagRepository>();

        // За замовчуванням ADO...
        builder.Services.AddScoped<ISubscriptionRepository>(_ => new AdoSubscriptionRepository(connectionString));
        builder.Services.AddScoped<ICategoryRepository>(_ => new AdoCategoryRepository(connectionString));
        builder.Services.AddScoped<IBillingRepository>(_ => new AdoBillingRepository(connectionString));

        // ...а EF-версії за ключем (для демонстрації та порівняння)
        builder.Services.AddKeyedScoped<ISubscriptionRepository, EFSubscriptionRepository>("ef");
        builder.Services.AddKeyedScoped<ICategoryRepository, EFCategoryRepository>("ef");
        builder.Services.AddKeyedScoped<IBillingRepository, EFBillingRepository>("ef");

        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        builder.Services.AddScoped<IChargeService, ChargeService>();

        builder.Services.AddValidatorsFromAssemblyContaining<UserService>(ServiceLifetime.Singleton);

        builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        var app = builder.Build();

        app.UseExceptionHandler();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
            await db.Database.MigrateAsync();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        await app.RunAsync();
    }
}
