using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.Categories.Common.Persistence.Configuration;

/// <inheritdoc />
internal class CategoryConfigurations : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.CategoryNumber).IsRequired();
        builder.Ignore(category => category.ParentCategoryNumber);

        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.Path).IsRequired();
        builder.Property(category => category.IsLeaf).IsRequired();

        builder.Ignore(category => category.IsSelected);

        builder
            .HasOne(category => category.RootCategory)
            .WithMany(rootCategory => rootCategory.Categories)
            .HasForeignKey(category => category.RootCategoryId)
            .IsRequired();

        builder
            .HasOne(category => category.Parent)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentId)
            .IsRequired(false);

        builder
            .HasMany(category => category.Attributes)
            .WithMany(attribute => attribute.Categories)
            .UsingEntity(j => j.ToTable("attributes_categories"));

        builder.HasIndex(nameof(Category.CategoryNumber), nameof(Category.RootCategoryId))
            .IsUnique();

        builder.HasIndex(nameof(Category.IsLeaf))
            .IsUnique(false);
    }
}
