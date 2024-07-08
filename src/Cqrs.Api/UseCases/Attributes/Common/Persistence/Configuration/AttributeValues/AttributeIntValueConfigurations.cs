using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Configuration.AttributeValues;

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
