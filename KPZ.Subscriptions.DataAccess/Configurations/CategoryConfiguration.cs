using KPZ.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPZ.Subscriptions.DataAccess.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("Categories");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
              .HasMaxLength(100)
              .IsRequired();

        entity.HasIndex(x => x.Name).IsUnique();

        entity.HasData(
            new Category { Id = 1, Name = "Стрімінг" },
            new Category { Id = 2, Name = "Музика" },
            new Category { Id = 3, Name = "Хмарні сервіси" },
            new Category { Id = 4, Name = "Освіта" },
            new Category { Id = 5, Name = "Ігри" },
            new Category { Id = 6, Name = "Комунальні та зв'язок" });
    }
}
