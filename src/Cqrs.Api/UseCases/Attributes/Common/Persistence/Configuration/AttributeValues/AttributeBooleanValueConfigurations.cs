using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Configuration.AttributeValues;

/// <inheritdoc />
internal class AttributeBooleanValueConfigurations : IEntityTypeConfiguration<AttributeBooleanValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttributeBooleanValue> builder)
    {
        builder
            .HasKey(attributeValue => attributeValue.Id);
    }
}
