using KPZ.Subscriptions.Domain.Entities;
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

        entity.HasIndex(x => x.Email).IsUnique();
    }
}
