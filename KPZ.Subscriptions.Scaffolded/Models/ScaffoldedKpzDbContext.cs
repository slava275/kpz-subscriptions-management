using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace KPZ.Subscriptions.Scaffolded.Models;

public partial class ScaffoldedKpzDbContext : DbContext
{
    public ScaffoldedKpzDbContext(DbContextOptions<ScaffoldedKpzDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Charge> Charges { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SubscriptionTag> SubscriptionTags { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_Categories_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Charge>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.DueDate }, "IX_Charges_Status_DueDate");

            entity.HasIndex(e => new { e.SubscriptionId, e.DueDate }, "IX_Charges_SubscriptionId_DueDate");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Subscription).WithMany(p => p.Charges).HasForeignKey(d => d.SubscriptionId);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderSubject }, "IX_ExternalLogins_Provider_ProviderSubject").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_ExternalLogins_UserId");

            entity.Property(e => e.LinkedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.ProviderEmail).HasMaxLength(256);
            entity.Property(e => e.ProviderSubject).HasMaxLength(256);

            entity.HasOne(d => d.User).WithMany(p => p.ExternalLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_PaymentMethods_UserId");

            entity.Property(e => e.Last4)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.PaymentMethods)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Subscriptions_CategoryId");

            entity.HasIndex(e => e.PaymentMethodId, "IX_Subscriptions_PaymentMethodId");

            entity.HasIndex(e => new { e.UserId, e.NextChargeDate }, "IX_Subscriptions_UserId_NextChargeDate");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Period).HasMaxLength(20);
            entity.Property(e => e.RemindBeforeDays).HasDefaultValue(3);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Category).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SubscriptionTag>(entity =>
        {
            entity.HasKey(e => new { e.SubscriptionId, e.TagId });

            entity.HasIndex(e => e.TagId, "IX_SubscriptionTags_TagId");

            entity.Property(e => e.AddedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Note).HasMaxLength(200);

            entity.HasOne(d => d.Subscription).WithMany(p => p.SubscriptionTags).HasForeignKey(d => d.SubscriptionId);

            entity.HasOne(d => d.Tag).WithMany(p => p.SubscriptionTags).HasForeignKey(d => d.TagId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Name }, "IX_Tags_UserId_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Tags)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.Role, "IX_Users_Role");

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
