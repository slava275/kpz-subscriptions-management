using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class SubscriptionTagConfiguration : IEntityTypeConfiguration<SubscriptionTag>
{
    public void Configure(EntityTypeBuilder<SubscriptionTag> entity)
    {
        entity.ToTable("SubscriptionTags");

        entity.HasKey(x => new { x.SubscriptionId, x.TagId });

        entity.Property(x => x.Note)
              .HasMaxLength(200);

        entity.Property(x => x.AddedAt)
              .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasOne(x => x.Subscription)
              .WithMany(s => s.SubscriptionTags)
              .HasForeignKey(x => x.SubscriptionId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Tag)
              .WithMany(t => t.SubscriptionTags)
              .HasForeignKey(x => x.TagId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.TagId);
    }
}