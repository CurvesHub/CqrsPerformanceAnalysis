using Cqrs.Api.Common.DataAccess.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Cqrs.Api.UseCases.Articles.Persistence.Entities;

/// <summary>
/// Entity to store articles.
/// </summary>
/// <param name="articleNumber">Sets the article number.</param>
/// <param name="characteristicId">Sets the characteristic id.</param>
public class Article(
    string articleNumber,
    int characteristicId)
    : BaseEntity
{
    /// <summary>
    /// Gets or sets the associated article number.
    /// </summary>
    public string ArticleNumber { get; set; } = articleNumber;

    /// <summary>
    /// Gets or sets the associated characteristic id.
    /// </summary>
    public int CharacteristicId { get; set; } = characteristicId;

    /// <summary>
    /// Gets or sets a collection of the associated <see cref="Category"/>s.
    /// </summary>
    public List<Category>? Categories { get; set; }

    /// <summary>
    /// Gets or sets a collection of the associated <see cref="AttributeBooleanValue"/>s.
    /// </summary>
    public List<AttributeBooleanValue>? AttributeBooleanValues { get; set; }

    /// <summary>
    /// Gets or sets a collection of the associated <see cref="AttributeDecimalValue"/>s.
    /// </summary>
    public List<AttributeDecimalValue>? AttributeDecimalValues { get; set; }

    /// <summary>
    /// Gets or sets a collection of the associated <see cref="AttributeIntValue"/>s.
    /// </summary>
    public List<AttributeIntValue>? AttributeIntValues { get; set; }

    /// <summary>
    /// Gets or sets a collection of the associated <see cref="AttributeStringValue"/>s.
    /// </summary>
    public List<AttributeStringValue>? AttributeStringValues { get; set; }
}
