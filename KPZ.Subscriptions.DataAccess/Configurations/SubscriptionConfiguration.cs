using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> entity)
    {
        entity.ToTable("Subscriptions", t =>
            t.HasCheckConstraint("CK_Subscriptions_Amount", "[Amount] > 0"));

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
              .HasMaxLength(200)
              .IsRequired();

        entity.Property(x => x.Notes)
              .HasMaxLength(1000);

        entity.Property(x => x.Amount)
              .HasPrecision(18, 2);

        entity.Property(x => x.Currency)
              .HasMaxLength(3)
              .IsFixedLength()
              .IsUnicode(false)
              .IsRequired();

        entity.Property(x => x.Period)
              .HasConversion<string>()
              .HasMaxLength(20);

        entity.Property(x => x.Status)
              .HasConversion<string>()
              .HasMaxLength(20)
              .HasDefaultValue(SubscriptionStatus.Active); 
        
        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(x => x.RemindBeforeDays)
              .HasDefaultValue(3);

        entity.Property(x => x.Version).IsRowVersion();

        entity.HasOne(x => x.User)
              .WithMany(u => u.Subscriptions)
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Category)
              .WithMany(c => c.Subscriptions)
              .HasForeignKey(x => x.CategoryId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.PaymentMethod)
              .WithMany(p => p.Subscriptions)
              .HasForeignKey(x => x.PaymentMethodId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.UserId, x.NextChargeDate });
        entity.HasIndex(x => x.CategoryId);
        entity.HasIndex(x => x.PaymentMethodId);
    }
}