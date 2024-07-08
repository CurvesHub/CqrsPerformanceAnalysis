using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Configuration.AttributeValues;

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
