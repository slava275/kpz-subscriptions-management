using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> entity)
    {
        entity.ToTable("PaymentMethods");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Title)
              .HasMaxLength(100)
              .IsRequired();

        entity.Property(x => x.Type)
              .HasConversion<string>()
              .HasMaxLength(20);

        entity.Property(x => x.Last4)
              .HasMaxLength(4)
              .IsFixedLength()
              .IsUnicode(false);

        entity.HasOne(x => x.User)
              .WithMany(u => u.PaymentMethods)
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => x.UserId);
    }
}