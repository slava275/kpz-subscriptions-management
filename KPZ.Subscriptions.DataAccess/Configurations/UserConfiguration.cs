using KPZ.Subscriptions.Domain.Entities;
using KPZ.Subscriptions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
              .HasMaxLength(100)
              .IsRequired();

        entity.Property(x => x.Email)
              .HasMaxLength(256)
              .IsRequired();

        entity.Property(x => x.CreatedAt)
              .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(x => x.Role)
          .HasConversion<string>()
          .HasMaxLength(20)
          .HasDefaultValue(UserRole.User);

        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.Property(x => x.AvatarUrl).HasMaxLength(500);
        entity.HasIndex(x => x.Role);

        entity.HasIndex(x => x.Email).IsUnique();
    }
}
