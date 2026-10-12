using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> entity)
    {
        entity.ToTable("Charges", t =>
            t.HasCheckConstraint("CK_Charges_Amount", "[Amount] > 0"));

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Amount)
              .HasPrecision(18, 2);

        entity.Property(x => x.Status)
              .HasConversion<string>()
              .HasMaxLength(20);

        entity.HasOne(x => x.Subscription)
              .WithMany(s => s.Charges)
              .HasForeignKey(x => x.SubscriptionId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.SubscriptionId, x.DueDate });
        entity.HasIndex(x => new { x.Status, x.DueDate });
    }
}