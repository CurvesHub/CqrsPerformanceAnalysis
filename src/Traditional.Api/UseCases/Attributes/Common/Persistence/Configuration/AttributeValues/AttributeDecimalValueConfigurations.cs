using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Configuration.AttributeValues;

/// <inheritdoc />
internal class AttributeDecimalValueConfigurations : IEntityTypeConfiguration<AttributeDecimalValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttributeDecimalValue> builder)
    {
        builder
            .HasKey(attributeValue => attributeValue.Id);
    }
}
