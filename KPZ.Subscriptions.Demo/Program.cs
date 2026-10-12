using Kpz.Subscriptions.Data.Ado;
using Kpz.Subscriptions.DataAccess.EF;
using KPZ.Subscriptions.DataAccess;
using KPZ.Subscriptions.DataAccess.Seeding;
using KPZ.Subscriptions.Domain.Abstractions;
using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using KPZ.Subscriptions.Domain.Exceptions;
using KPZ.Subscriptions.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

var connectionString = "Server=.;Database=KpzSubscriptionsDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

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
using var host = builder.Build();

var pause = !args.Contains("--no-pause");
var ct = CancellationToken.None;

await Run("0. Початковий стан даних", () => S0_InitialState(host.Services, ct));
await Run("1. ADO.NET CRUD (Category, Subscription)", () => S1_AdoCrud(host.Services, ct));
await Run("2. ADO.NET: JOIN трьох таблиць та GROUP BY", () => S2_AdoQueries(host.Services, ct));
await Run("3. ADO.NET транзакція: Commit і Rollback", () => S3_AdoTransaction(host.Services, ct));
await Run("4. EF Core CRUD та транзакційна операція над entity graph", () => S4_EfCrud(host.Services, ct));
await Run("5. Пагінація, фільтрація, сортування (EF Core)", () => S5_Paging(host.Services, ct));
await Run("6. Optimistic concurrency (rowversion)", () => S6_Concurrency(host.Services, ct));
await Run("7. SQL, який генерує EF Core", () => S7_Sql(host.Services, connectionString, ct));

Console.WriteLine("\nДемонстрацію завершено.");

// ======================================================================
async Task Run(string title, Func<Task> body)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n{new string('=', 70)}\n{title}\n{new string('=', 70)}");
    Console.ResetColor();
    try { await body(); }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ПОМИЛКА СЕКЦІЇ: {ex.GetType().Name}: {ex.Message}");
        Console.ResetColor();
    }
    if (pause) { Console.Write("\n[Enter] — далі... "); Console.ReadLine(); }
}

static void Info(string text) => Console.WriteLine("  " + text);
static void Ok(string text) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  ✔ " + text); Console.ResetColor(); }
static void Warn(string text) { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("  ! " + text); Console.ResetColor(); }
static void Print(SubscriptionListItem i) =>
    Console.WriteLine($"  #{i.Id,-3} {i.Name,-28} {i.CategoryName,-22} {i.PaymentMethodTitle,-20} " +
                      $"{i.Amount,8:F2} {i.Currency} {i.Period,-9} {i.NextChargeDate:yyyy-MM-dd} {i.Status}");

// Власник для тестових записів: перший користувач із способом оплати
static async Task<(int userId, int pmId)> PickOwner(SubscriptionsDbContext db, CancellationToken ct)
{
    var pm = await db.PaymentMethods
        .AsNoTracking()
        .OrderBy(p => p.Id)
        .FirstAsync(ct);

    return (pm.UserId, pm.Id);
}

// ---------------------------------------------------------------- 0
static async Task S0_InitialState(IServiceProvider root, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();

    Info($"Categories:       {await db.Categories.CountAsync(ct)}");
    Info($"Users:            {await db.Users.CountAsync(ct)}");
    Info($"PaymentMethods:   {await db.PaymentMethods.CountAsync(ct)}");
    Info($"Tags:             {await db.Tags.CountAsync(ct)}");
    Info($"Subscriptions:    {await db.Subscriptions.CountAsync(ct)}");
    Info($"Charges:          {await db.Charges.CountAsync(ct)}");
    Info($"SubscriptionTags: {await db.SubscriptionTags.CountAsync(ct)}");

    Info("Підписки за статусами:");
    var byStatus = await db.Subscriptions
        .GroupBy(s => s.Status)
        .Select(g => new { g.Key, Count = g.Count() })
        .ToListAsync(ct);

    foreach (var x in byStatus) Info($"  {x.Key}: {x.Count}");
}

// ---------------------------------------------------------------- 1
static async Task S1_AdoCrud(IServiceProvider root, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<SubscriptionsDbContext>();
    var cats = sp.GetRequiredService<ICategoryRepository>();       // ADO
    var subs = sp.GetRequiredService<ISubscriptionRepository>();   // ADO

    // --- Category ---
    Info("[Category] CREATE");
    foreach (var old in (await cats.GetAllAsync(ct)).Where(c => c.Name.StartsWith("Демо")))
        await cats.DeleteAsync(old.Id, ct);                        // прибираємо залишки попереднього запуску

    var catId = await cats.AddAsync(new Category { Name = "Демо-категорія" }, ct);
    Ok($"Додано, Id = {catId}");

    Info("[Category] READ");
    var cat = await cats.GetByIdAsync(catId, ct);
    Ok($"{cat!.Id}: {cat.Name}");

    Info("[Category] UPDATE");
    cat.Name = "Демо-категорія (змінена)";
    Ok($"Оновлено: {await cats.UpdateAsync(cat, ct)}");

    Info("[Category] дублікат імені (UNIQUE)");
    try { await cats.AddAsync(new Category { Name = cat.Name }, ct); }
    catch (DuplicateException ex) { Ok($"DuplicateException: {ex.Message}"); }

    Info("[Category] DELETE категорії з підписками (Restrict, FK 547)");
    try { await cats.DeleteAsync(1, ct); }
    catch (HasDependenciesException ex) { Ok($"HasDependenciesException: {ex.Message}"); }

    // --- Subscription ---
    var (userId, pmId) = await PickOwner(db, ct);
    var today = DateOnly.FromDateTime(DateTime.UtcNow);

    Info("[Subscription] CREATE");
    var subId = await subs.AddAsync(new Subscription
    {
        Name = "Демо-підписка",
        Notes = "створено через ADO.NET",
        Amount = 123.45m,
        Currency = "UAH",
        Period = BillingPeriod.Monthly,
        StartDate = today,
        NextChargeDate = today.AddMonths(1),
        RemindBeforeDays = 2,
        Status = SubscriptionStatus.Active,
        UserId = userId,
        CategoryId = catId,
        PaymentMethodId = pmId
    }, ct);
    Ok($"Додано, Id = {subId}");

    Info("[Subscription] READ");
    var sub = await subs.GetByIdAsync(subId, ct);
    Ok($"{sub!.Name}, {sub.Amount} {sub.Currency}, наступне списання {sub.NextChargeDate}, Version = 0x{Convert.ToHexString(sub.Version)}");

    Info("[Subscription] UPDATE");
    sub.Amount = 150m;
    sub.Status = SubscriptionStatus.Paused;
    Ok($"Оновлено: {await subs.UpdateAsync(sub, ct)}");
    var after = await subs.GetByIdAsync(subId, ct);
    Ok($"Після оновлення: {after!.Amount} {after.Status}, нова Version = 0x{Convert.ToHexString(after.Version)}");

    Info("[Subscription] DELETE");
    Ok($"Видалено підписку: {await subs.DeleteAsync(subId, ct)}");
    Ok($"Видалено категорію: {await cats.DeleteAsync(catId, ct)}");
}

// ---------------------------------------------------------------- 2
static async Task S2_AdoQueries(IServiceProvider root, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<SubscriptionsDbContext>();
    var subs = sp.GetRequiredService<ISubscriptionRepository>();   // ADO
    var (userId, _) = await PickOwner(db, ct);

    Info("JOIN Subscriptions + Categories + PaymentMethods; фільтр: сума ≥ 100, сортування: сума за спаданням");
    var page = await subs.SearchAsync(new SubscriptionFilter(
        MinAmount: 100m, SortBy: "amount", Descending: true, Page: 1, PageSize: 8), ct);
    Info($"Всього: {page.TotalCount}, сторінка {page.Page}, показано {page.Items.Count}");
    foreach (var i in page.Items) Print(i);

    Info("\nКалендар списань користувача на найближчі 30 днів:");
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    foreach (var i in await subs.GetUpcomingAsync(userId, today, today.AddDays(30), ct)) Print(i);

    Info("\nПідписки, про які пора нагадати:");
    foreach (var i in await subs.GetDueForReminderAsync(today, ct)) Print(i);

    Info($"\nGROUP BY: витрати активних підписок користувача #{userId} за категоріями");
    Info($"{"Категорія",-24}{"К-ть",5}{"Сума",12}{"Середнє",12}");
    foreach (var r in await subs.GetSpendingByCategoryAsync(userId, 0, ct))
        Info($"{r.CategoryName,-24}{r.SubscriptionCount,5}{r.TotalAmount,12:F2}{(r.AverageAmount?.ToString("F2") ?? "—"),12}");
}

// ---------------------------------------------------------------- 3
static async Task S3_AdoTransaction(IServiceProvider root, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<SubscriptionsDbContext>();
    var subs = sp.GetRequiredService<ISubscriptionRepository>();       // ADO
    var billing = sp.GetRequiredService<IBillingRepository>();         // ADO
    var efSubs = sp.GetRequiredKeyedService<ISubscriptionRepository>("ef");

    // ---------- COMMIT ----------
    Info("СЦЕНАРІЙ COMMIT: оплата планового списання");
    var charge = await db.Charges.AsNoTracking()
        .Where(c => c.Status == ChargeStatus.Planned && c.Subscription.Status == SubscriptionStatus.Active)
        .OrderBy(c => c.Id).FirstAsync(ct);

    var before = (await subs.GetWithDetailsAsync(charge.SubscriptionId, ct))!;
    Info($"ДО:    підписка «{before.Name}», NextChargeDate = {before.NextChargeDate}, списань = {before.Charges.Count}, " +
         $"Planned = {before.Charges.Count(c => c.Status == ChargeStatus.Planned)}");

    var result = await billing.PayChargeAsync(charge.Id, DateTime.UtcNow, ct);
    Ok($"Результат: {result}");

    var afterCommit = (await subs.GetWithDetailsAsync(charge.SubscriptionId, ct))!;
    Info($"ПІСЛЯ: NextChargeDate = {afterCommit.NextChargeDate}, списань = {afterCommit.Charges.Count}, " +
         $"Planned = {afterCommit.Charges.Count(c => c.Status == ChargeStatus.Planned)}");
    Ok($"Charge #{charge.Id} тепер: {afterCommit.Charges.First(c => c.Id == charge.Id).Status}");

    Info("Повторна оплата того самого списання:");
    Ok($"Результат: {await billing.PayChargeAsync(charge.Id, DateTime.UtcNow, ct)}");

    // ---------- ROLLBACK ----------
    Info("\nСЦЕНАРІЙ ROLLBACK: помилка всередині транзакції");
    Info("Підписка з NextChargeDate = 9999-12-31: обчислення наступної дати кидає виняток");
    Info("ПІСЛЯ того, як Charge уже позначено Paid (крок 3 з 5). Транзакція має це скасувати.");

    var (userId, pmId) = await PickOwner(db, ct);
    var max = DateOnly.MaxValue;
    var badId = await efSubs.AddAsync(new Subscription
    {
        Name = "Демо-Rollback",
        Amount = 10m,
        Currency = "USD",
        Period = BillingPeriod.Monthly,
        StartDate = max,
        NextChargeDate = max,
        RemindBeforeDays = 1,
        Status = SubscriptionStatus.Active,
        UserId = userId,
        CategoryId = 1,
        PaymentMethodId = pmId,
        Charges = { new Charge { DueDate = max, Amount = 10m, Status = ChargeStatus.Planned } }
    }, ct);

    var badCharge = (await subs.GetWithDetailsAsync(badId, ct))!.Charges.Single();
    Info($"ДО:    Charge #{badCharge.Id} = {badCharge.Status}");
    try
    {
        await billing.PayChargeAsync(badCharge.Id, DateTime.UtcNow, ct);
        Warn("Виняток не виник — демонстрація не спрацювала");
    }
    catch (Exception ex)
    {
        Ok($"Виняток: {ex.GetType().Name} → викликано Rollback");
    }
    var afterRollback = (await subs.GetWithDetailsAsync(badId, ct))!;
    var st = afterRollback.Charges.Single().Status;
    Info($"ПІСЛЯ: Charge #{badCharge.Id} = {st}, списань = {afterRollback.Charges.Count}");
    if (st == ChargeStatus.Planned && afterRollback.Charges.Count == 1) Ok("Зміни скасовано, дані не змінились");
    else Warn("Rollback не спрацював!");

    await subs.DeleteAsync(badId, ct);
}

// ---------------------------------------------------------------- 4
static async Task S4_EfCrud(IServiceProvider root, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var sp = scope.ServiceProvider;
    var users = sp.GetRequiredService<IUserRepository>();
    var methods = sp.GetRequiredService<IPaymentMethodRepository>();
    var tags = sp.GetRequiredService<ITagRepository>();
    var subs = sp.GetRequiredKeyedService<ISubscriptionRepository>("ef");
    var billing = sp.GetRequiredKeyedService<IBillingRepository>("ef");

    var email = $"demo.{Guid.NewGuid():N}@example.com";

    // --- User ---
    Info("[User] CREATE / READ / UPDATE");
    var userId = await users.AddAsync(new User { Name = "Демо Користувач", Email = email, CreatedAt = DateTime.UtcNow }, ct);
    Ok($"Додано користувача #{userId}");
    var u = (await users.GetByIdAsync(userId, ct))!;
    u.Name = "Демо Користувач (змінений)";
    Ok($"Оновлено: {await users.UpdateAsync(u, ct)}");

    Info("[User] дублікат email (UNIQUE INDEX)");
    try { await users.AddAsync(new User { Name = "Дубль", Email = email, CreatedAt = DateTime.UtcNow }, ct); }
    catch (DuplicateException ex) { Ok($"DuplicateException: {ex.Message}"); }

    // --- PaymentMethod ---
    Info("[PaymentMethod] CREATE");
    var pmId = await methods.AddAsync(new PaymentMethod
    { UserId = userId, Title = "Демо картка *0001", Type = PaymentMethodType.Card, Last4 = "0001" }, ct);
    Ok($"Додано спосіб оплати #{pmId}");

    // --- Tag ---
    Info("[Tag] CREATE");
    var tagId = await tags.AddAsync(new Tag { UserId = userId, Name = "демо" }, ct);
    Ok($"Додано тег #{tagId}");
    Ok($"Тег уже існує? {await tags.ExistsAsync(userId, "демо", ct)}");

    // --- Entity graph ---
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    Info("\n[Entity graph] Підписка + Charge + SubscriptionTag одним SaveChanges");
    var subId = await subs.AddAsync(new Subscription
    {
        Name = "Демо EF-підписка",
        Amount = 99m,
        Currency = "UAH",
        Period = BillingPeriod.Monthly,
        StartDate = today,
        NextChargeDate = today.AddDays(5),
        RemindBeforeDays = 3,
        Status = SubscriptionStatus.Active,
        UserId = userId,
        CategoryId = 2,
        PaymentMethodId = pmId,
        Charges = { new Charge { DueDate = today.AddDays(5), Amount = 99m, Status = ChargeStatus.Planned } },
        SubscriptionTags = { new SubscriptionTag { TagId = tagId, AddedAt = DateTime.UtcNow, Note = "через entity graph" } }
    }, ct);
    Ok($"Додано підписку #{subId} разом із залежними записами");

    var graph = (await subs.GetWithDetailsAsync(subId, ct))!;
    Info($"Include/ThenInclude: списань = {graph.Charges.Count}, тегів = {graph.SubscriptionTags.Count} " +
         $"(«{graph.SubscriptionTags.First().Tag.Name}», нотатка: {graph.SubscriptionTags.First().Note})");

    // --- Складна бізнес-операція в транзакції ---
    Info("\n[Транзакційна операція] EfBillingRepository.PayChargeAsync");
    var chargeId = graph.Charges.Single().Id;
    Ok($"Результат: {await billing.PayChargeAsync(chargeId, DateTime.UtcNow, ct)}");
    var paid = (await subs.GetWithDetailsAsync(subId, ct))!;
    Info($"NextChargeDate: {graph.NextChargeDate} → {paid.NextChargeDate}; списань: {graph.Charges.Count} → {paid.Charges.Count}");
    foreach (var c in paid.Charges) Info($"  Charge #{c.Id}: {c.DueDate} {c.Status}");

    // --- Delete behavior ---
    Info("\n[Delete behavior]");
    try { await users.DeleteAsync(userId, ct); }
    catch (HasDependenciesException ex) { Ok($"User → Restrict: {ex.Message}"); }

    Ok($"Subscription → Cascade (Charges, SubscriptionTags): видалено = {await subs.DeleteAsync(subId, ct)}");
    Ok($"Tag видалено: {await tags.DeleteAsync(tagId, ct)}");
    Ok($"PaymentMethod видалено: {await methods.DeleteAsync(pmId, ct)}");
    Ok($"User видалено: {await users.DeleteAsync(userId, ct)}");
}

// ---------------------------------------------------------------- 5
static async Task S5_Paging(IServiceProvider root, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var sp = scope.ServiceProvider;
    var subs = sp.GetRequiredKeyedService<ISubscriptionRepository>("ef");
    var charges = sp.GetRequiredService<IChargeRepository>();

    Info("Підписки, сортування за назвою, 4 на сторінку");
    for (var p = 1; p <= 2; p++)
    {
        var page = await subs.SearchAsync(new SubscriptionFilter(SortBy: "name", Page: p, PageSize: 4), ct);
        Info($"--- сторінка {page.Page} з {(int)Math.Ceiling(page.TotalCount / (double)page.PageSize)} (всього {page.TotalCount}) ---");
        foreach (var i in page.Items) Print(i);
    }

    Info("\nФільтр: Active + пошук «o» + категорія за спаданням суми");
    var f = await subs.SearchAsync(new SubscriptionFilter(
        Status: SubscriptionStatus.Active, Search: "o", SortBy: "amount", Descending: true, PageSize: 10), ct);
    Info($"Знайдено: {f.TotalCount}");
    foreach (var i in f.Items) Print(i);

    Info("\nПагінація списань (Paid) однієї підписки");
    var first = (await subs.SearchAsync(new SubscriptionFilter(PageSize: 1), ct)).Items[0];
    var cp = await charges.GetPagedAsync(first.Id, ChargeStatus.Paid, 1, 2, ct);
    Info($"Підписка «{first.Name}»: оплачених списань {cp.TotalCount}, показано {cp.Items.Count}");
    foreach (var c in cp.Items) Info($"  #{c.Id} {c.DueDate} {c.Amount:F2} {c.Status}");
}

// ---------------------------------------------------------------- 6
static async Task S6_Concurrency(IServiceProvider root, CancellationToken ct)
{
    await Demo("EF Core", key: "ef");
    await Demo("ADO.NET", key: null);

    async Task Demo(string label, string? key)
    {
        Info($"\n--- {label} ---");
        // Два незалежні scope = два окремі DbContext / з'єднання
        await using var scopeA = root.CreateAsyncScope();
        await using var scopeB = root.CreateAsyncScope();
        var repoA = key is null ? scopeA.ServiceProvider.GetRequiredService<ISubscriptionRepository>()
                                : scopeA.ServiceProvider.GetRequiredKeyedService<ISubscriptionRepository>(key);
        var repoB = key is null ? scopeB.ServiceProvider.GetRequiredService<ISubscriptionRepository>()
                                : scopeB.ServiceProvider.GetRequiredKeyedService<ISubscriptionRepository>(key);

        var id = (await repoA.SearchAsync(new SubscriptionFilter(PageSize: 1), ct)).Items[0].Id;

        var a = (await repoA.GetByIdAsync(id, ct))!;
        var b = (await repoB.GetByIdAsync(id, ct))!;
        var origNotes = a.Notes; var origRemind = a.RemindBeforeDays;
        Info($"A і B прочитали підписку #{id}, Version = 0x{Convert.ToHexString(a.Version)}");

        a.Notes = "змінено користувачем A";
        await repoA.UpdateAsync(a, ct);
        Ok("A зберіг зміни (Version на сервері змінилась)");

        b.RemindBeforeDays = origRemind + 1;
        try
        {
            await repoB.UpdateAsync(b, ct);
            Warn("B зберіг без конфлікту — перевірка не спрацювала!");
        }
        catch (ConcurrencyConflictException ex)
        {
            Ok($"B отримав ConcurrencyConflictException: {ex.Message}");
            // Реакція застосунку: показати актуальні значення й повторити зміну над свіжою версією
            var fresh = (await repoB.GetByIdAsync(id, ct))!;
            Info($"Актуальні значення: Notes = «{fresh.Notes}», Version = 0x{Convert.ToHexString(fresh.Version)}");
            fresh.RemindBeforeDays = origRemind + 1;
            await repoB.UpdateAsync(fresh, ct);
            Ok("B повторив свою зміну над актуальною версією — обидві зміни збережено");
        }

        // Повертаємо початкові значення
        var restore = (await repoA.GetByIdAsync(id, ct))!;
        restore.Notes = origNotes;
        restore.RemindBeforeDays = origRemind;
        await repoA.UpdateAsync(restore, ct);
    }
}

// ---------------------------------------------------------------- 7
static async Task S7_Sql(IServiceProvider root, string cs, CancellationToken ct)
{
    using var scope = root.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();

    // 1) Фільтр + сортування + DTO-проєкція (JOIN береться з навігацій у Select)
    var q1 = db.Subscriptions.AsNoTracking()
        .Where(s => s.Status == SubscriptionStatus.Active && s.Amount >= 100m)
        .OrderBy(s => s.Name)
        .Select(s => new SubscriptionListItem(
            s.Id, s.Name, s.Category.Name, s.PaymentMethod.Title,
            s.Amount, s.Currency, s.Period, s.NextChargeDate, s.Status));
    Info("ЗАПИТ 1: Where + OrderBy + Select (DTO-проєкція)");
    Info(q1.ToQueryString());

    // 2) Related data
    var q2 = db.Subscriptions.AsNoTracking()
        .Include(s => s.Charges)
        .Include(s => s.SubscriptionTags).ThenInclude(st => st.Tag)
        .Where(s => s.UserId == 1);
    Info("\nЗАПИТ 2: Include + ThenInclude (одним запитом, без AsSplitQuery)");
    Info(q2.ToQueryString());

    // 3) Агрегація навігаційної колекції
    var q3 = db.Categories.AsNoTracking()
        .Select(c => new
        {
            c.Name,
            Count = c.Subscriptions.Count(),
            Total = c.Subscriptions.Sum(s => s.Amount),
            Avg = c.Subscriptions.Average(s => (decimal?)s.Amount)
        })
        .OrderByDescending(x => x.Total);
    Info("\nЗАПИТ 3: агрегати по навігаційній колекції");
    Info(q3.ToQueryString());

    // N+1: окремий контекст з логуванням виконаних команд
    Info("\nПЕРЕВІРКА N+1: EF-пошук підписок із логуванням команд");
    var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
        .UseSqlServer(cs)
        .LogTo(msg => Console.WriteLine("    [SQL] " + msg.Split('\n')[0]),
               [RelationalEventId.CommandExecuted], LogLevel.Information)
        .Options;

    await using var logged = new SubscriptionsDbContext(options);
    var repo = new EFSubscriptionRepository(logged);
    var page = await repo.SearchAsync(new SubscriptionFilter(SortBy: "category", PageSize: 5), ct);
    Info($"Отримано {page.Items.Count} рядків (потрібна назва категорії та способу оплати для кожного).");
    Info("Очікується рівно 2 команди (COUNT + SELECT з JOIN) незалежно від кількості рядків — N+1 немає.");
}