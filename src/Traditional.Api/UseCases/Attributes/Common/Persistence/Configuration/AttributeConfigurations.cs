using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;
using Persistence_Entities_Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Configuration;

/// <inheritdoc />
internal class AttributeConfigurations : IEntityTypeConfiguration<Persistence_Entities_Attribute>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Attribute> builder)
    {
        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.Name).IsRequired();
        builder.Property(attribute => attribute.ValueType).IsRequired();
        builder.Property(attribute => attribute.MinValues).IsRequired();
        builder.Property(attribute => attribute.MaxValues).IsRequired();

        builder.Property(attribute => attribute.MarketplaceAttributeIds).IsRequired();

        builder.Property(attribute => attribute.AllowedValues).IsRequired(false);

        builder.Property(attribute => attribute.MinLength)
            .HasPrecision(24, 6)
            .HasColumnType("numeric(24,6)")
            .IsRequired(false);

        builder.Property(attribute => attribute.MaxLength)
            .HasPrecision(24, 6)
            .HasColumnType("numeric(24,6)")
            .IsRequired(false);

        const string npgsqlComputedColumn = "SUBSTR(\"marketplace_attribute_ids\"::TEXT, 1,\n(POSITION(',' IN \"marketplace_attribute_ids\") - 1 + (1 - ROUND(POSITION(',' IN \"marketplace_attribute_ids\") / (1.0 * POSITION(',' IN \"marketplace_attribute_ids\") + 1))) * (LENGTH(\"marketplace_attribute_ids\") + 1))::Integer)";
        builder
            .Property(attribute => attribute.ProductType)
            .HasComputedColumnSql(npgsqlComputedColumn, stored: true)
            .IsRequired();

        builder.Property(attribute => attribute.IsEditable).IsRequired();
        builder.Property(attribute => attribute.ExampleValues).IsRequired(false);
        builder.Property(attribute => attribute.Description).IsRequired(false);

        builder.HasOne(attribute => attribute.ParentAttribute)
            .WithMany(attribute => attribute.SubAttributes)
            .HasForeignKey(attribute => attribute.ParentAttributeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(attribute => attribute.RootCategory)
            .WithMany(rootCategory => rootCategory.Attributes)
            .HasForeignKey(attribute => attribute.RootCategoryId)
            .IsRequired();

        builder.HasIndex(attribute => attribute.ProductType)
            .IsUnique(false);

        builder.HasIndex(attribute => attribute.MarketplaceAttributeIds)
            .IsUnique(false);

        builder.HasIndex(attribute => attribute.ValueType)
            .IsUnique(false);
    }
}
