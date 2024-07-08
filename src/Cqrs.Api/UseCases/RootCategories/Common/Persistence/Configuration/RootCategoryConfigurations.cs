using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.RootCategories.Common.Persistence.Configuration;

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "It is more readable if the seeding data is at the end of the class")]
internal class RootCategoryConfigurations : IEntityTypeConfiguration<RootCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RootCategory> builder)
    {
        builder.HasKey(rootCategory => rootCategory.Id);

        builder.Property(rootCategory => rootCategory.LocaleCode).IsRequired();

        builder
            .HasMany(rootCategory => rootCategory.Categories)
            .WithOne(category => category.RootCategory)
            .HasForeignKey(category => category.RootCategoryId);

        builder.HasData(GetSeedingData());
    }

    /// <summary>
    /// Provides the seeding data for the root categories.
    /// </summary>
    /// <returns>An array of root categories.</returns>
    internal static RootCategory[] GetSeedingData()
    {
        return
        [
            new RootCategory(LocaleCode.de_DE) { Id = 1 },
            new RootCategory(LocaleCode.fr_FR) { Id = 2 },
            new RootCategory(LocaleCode.nl_NL) { Id = 3 },
            new RootCategory(LocaleCode.es_ES) { Id = 4 },
            new RootCategory(LocaleCode.en_GB) { Id = 5 },
            new RootCategory(LocaleCode.it_IT) { Id = 6 },
            new RootCategory(LocaleCode.pl_PL) { Id = 7 },
            new RootCategory(LocaleCode.sv_SE) { Id = 8 }
        ];
    }
}
