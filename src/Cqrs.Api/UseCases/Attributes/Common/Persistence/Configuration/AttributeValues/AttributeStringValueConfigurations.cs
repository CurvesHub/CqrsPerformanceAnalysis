using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Configuration.AttributeValues;

/// <inheritdoc />
internal class AttributeStringValueConfigurations : IEntityTypeConfiguration<AttributeStringValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttributeStringValue> builder)
    {
        builder
            .HasKey(attributeValue => attributeValue.Id);
    }
}
