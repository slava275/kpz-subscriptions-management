using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> entity)
    {
        entity.ToTable("Tags");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
              .HasMaxLength(50)
              .IsRequired();

        entity.HasOne(x => x.User)
              .WithMany(u => u.Tags)
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
    }
}