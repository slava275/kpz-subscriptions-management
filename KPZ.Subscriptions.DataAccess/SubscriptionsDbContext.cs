using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KPZ.Subscriptions.DataAccess;

public sealed class SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SubscriptionTag> SubscriptionTags => Set<SubscriptionTag>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionsDbContext).Assembly);
}
