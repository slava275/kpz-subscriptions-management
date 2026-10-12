using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;

namespace KPZ.Subscriptions.DataAccess.Seeding;

public static class SeedData
{
    /// <summary>Будує граф: користувачі → способи оплати, теги, підписки → списання, теги підписок.</summary>
    public static List<User> BuildUsers()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // ---------- Користувачі ----------
        var olena = new User { Name = "Олена Коваль", Email = "olena.koval@example.com", CreatedAt = now, Role = UserRole.Admin };
        var taras = new User { Name = "Тарас Мельник", Email = "taras.melnyk@example.com", CreatedAt = now };
        var maria = new User { Name = "Марія Шевчук", Email = "maria.shevchuk@example.com", CreatedAt = now };

        // ---------- Способи оплати (5) ----------
        var olenaMono = Pm(olena, "Monobank *4421", PaymentMethodType.Card, "4421");
        var olenaPaypal = Pm(olena, "PayPal", PaymentMethodType.PayPal, null);
        var tarasPrivat = Pm(taras, "ПриватБанк *9087", PaymentMethodType.Card, "9087");
        var tarasBank = Pm(taras, "Банківський переказ", PaymentMethodType.BankTransfer, null);
        var mariaCard = Pm(maria, "Visa *1156", PaymentMethodType.Card, "1156");

        // ---------- Теги (8) ----------
        var oFamily = Tag(olena, "сімейне");
        var oWork = Tag(olena, "робота");
        var oEntert = Tag(olena, "розваги");
        var tFamily = Tag(taras, "сімейне");
        var tDev = Tag(taras, "розробка");
        var tLearn = Tag(taras, "навчання");
        var mEntert = Tag(maria, "розваги");
        var mSave = Tag(maria, "можна скасувати");

        // ---------- Підписки (15) ----------
        // Категорії: 1 Стрімінг, 2 Музика, 3 Хмарні, 4 Освіта, 5 Ігри, 6 Комунальні та зв'язок
        // Олена
        Sub(olena, olenaMono, "Netflix", 299m, "UAH", BillingPeriod.Monthly, 1, 7, SubscriptionStatus.Active, today, oFamily, oEntert);
        Sub(olena, olenaMono, "Spotify Family", 179m, "UAH", BillingPeriod.Monthly, 2, 9, SubscriptionStatus.Active, today, oFamily, oEntert);
        Sub(olena, olenaPaypal, "Google One 2TB", 9.99m, "USD", BillingPeriod.Monthly, 3, 6, SubscriptionStatus.Active, today, oWork);
        Sub(olena, olenaMono, "Duolingo Plus", 59.99m, "USD", BillingPeriod.Yearly, 4, 14, SubscriptionStatus.Active, today, oWork);
        Sub(olena, olenaMono, "Київстар домашній інтернет", 250m, "UAH", BillingPeriod.Monthly, 6, 8, SubscriptionStatus.Active, today);

        // Тарас
        Sub(taras, tarasPrivat, "GitHub Copilot", 10m, "USD", BillingPeriod.Monthly, 3, 8, SubscriptionStatus.Active, today, tDev);
        Sub(taras, tarasPrivat, "JetBrains All Products", 289m, "USD", BillingPeriod.Yearly, 3, 13, SubscriptionStatus.Active, today, tDev);
        Sub(taras, tarasPrivat, "YouTube Premium", 99m, "UAH", BillingPeriod.Monthly, 1, 6, SubscriptionStatus.Paused, today, tFamily);
        Sub(taras, tarasBank, "Coursera Plus", 59m, "USD", BillingPeriod.Monthly, 4, 5, SubscriptionStatus.Active, today, tLearn);
        Sub(taras, tarasPrivat, "Xbox Game Pass", 349m, "UAH", BillingPeriod.Monthly, 5, 7, SubscriptionStatus.Cancelled, today, tFamily);
        Sub(taras, tarasBank, "Lifecell мобільний зв'язок", 220m, "UAH", BillingPeriod.Monthly, 6, 9, SubscriptionStatus.Active, today);

        // Марія
        Sub(maria, mariaCard, "Disney+", 189m, "UAH", BillingPeriod.Monthly, 1, 6, SubscriptionStatus.Active, today, mEntert, mSave);
        Sub(maria, mariaCard, "Apple Music", 99m, "UAH", BillingPeriod.Monthly, 2, 8, SubscriptionStatus.Active, today, mEntert);
        Sub(maria, mariaCard, "iCloud 200GB", 99m, "UAH", BillingPeriod.Monthly, 3, 7, SubscriptionStatus.Active, today);
        Sub(maria, mariaCard, "Skillshare", 168m, "USD", BillingPeriod.Yearly, 4, 15, SubscriptionStatus.Cancelled, today, mSave);

        return [olena, taras, maria];
    }

    // ---------- Хелпери ----------

    private static PaymentMethod Pm(User u, string title, PaymentMethodType type, string? last4)
    {
        var pm = new PaymentMethod { Title = title, Type = type, Last4 = last4 };
        u.PaymentMethods.Add(pm);
        return pm;
    }

    private static Tag Tag(User u, string name)
    {
        var tag = new Tag { Name = name };
        u.Tags.Add(tag);
        return tag;
    }

    private static void Sub(
        User user, PaymentMethod pm, string name, decimal amount, string currency,
        BillingPeriod period, int categoryId, int startedMonthsAgo,
        SubscriptionStatus status, DateOnly today, params Tag[] tags)
    {
        var start = today.AddMonths(-startedMonthsAgo).AddDays(-3); // щоб дати не збігалися з сьогоднішньою

        // Усі дати списань від початку; ті, що < today, вже були
        var dates = new List<DateOnly>();
        var d = start;
        while (d < today)
        {
            dates.Add(d);
            d = Advance(d, period);
        }
        var next = d; // перше списання >= today

        var sub = new Subscription
        {
            Name = name,
            Amount = amount,
            Currency = currency,
            Period = period,
            StartDate = start,
            NextChargeDate = next,
            EndDate = status == SubscriptionStatus.Cancelled ? today.AddDays(-5) : null,
            RemindBeforeDays = 3,
            Status = status,
            CategoryId = categoryId,   // існує завдяки HasData
            PaymentMethod = pm
        };

        // Історія: останні 4 списання. Для скасованої підписки останнє списання
        // було до дати скасування, тому всі вони Paid.
        foreach (var due in dates.TakeLast(4))
        {
            sub.Charges.Add(new Charge
            {
                DueDate = due,
                Amount = amount,
                Status = ChargeStatus.Paid,
                PaidAt = due.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc)
            });
        }

        // Майбутнє списання планується тільки для активних підписок
        if (status == SubscriptionStatus.Active)
        {
            sub.Charges.Add(new Charge
            {
                DueDate = next,
                Amount = amount,
                Status = ChargeStatus.Planned
            });
        }

        foreach (var t in tags)
        {
            sub.SubscriptionTags.Add(new SubscriptionTag
            {
                Tag = t,
                AddedAt = DateTime.UtcNow,
                Note = null
            });
        }

        user.Subscriptions.Add(sub);
    }

    private static DateOnly Advance(DateOnly d, BillingPeriod p) => p switch
    {
        BillingPeriod.Weekly => d.AddDays(7),
        BillingPeriod.Monthly => d.AddMonths(1),
        BillingPeriod.Quarterly => d.AddMonths(3),
        BillingPeriod.Yearly => d.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(p))
    };
}
