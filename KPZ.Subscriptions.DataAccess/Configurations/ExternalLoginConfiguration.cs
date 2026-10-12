using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> entity)
    {
        entity.ToTable("ExternalLogins");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        entity.Property(x => x.ProviderSubject).HasMaxLength(256).IsRequired();
        entity.Property(x => x.ProviderEmail).HasMaxLength(256);
        entity.Property(x => x.LinkedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasOne(x => x.User)
              .WithMany(u => u.ExternalLogins)
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
        entity.HasIndex(x => x.UserId);
    }
}
