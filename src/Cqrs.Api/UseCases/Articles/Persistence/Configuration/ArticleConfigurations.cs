using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.Articles.Persistence.Configuration;

/// <inheritdoc />
internal class ArticleConfigurations : IEntityTypeConfiguration<Article>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder
            .HasKey(article => article.Id);

        builder
            .HasIndex(nameof(Article.ArticleNumber), nameof(Article.CharacteristicId))
            .IsUnique();

        builder
            .HasMany(article => article.Categories)
            .WithMany(category => category.Articles);
    }
}
