using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Configuration.AttributeValues;

/// <inheritdoc />
internal class AttributeIntValueConfigurations : IEntityTypeConfiguration<AttributeIntValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttributeIntValue> builder)
    {
        builder
            .HasKey(attributeValue => attributeValue.Id);
    }
}
