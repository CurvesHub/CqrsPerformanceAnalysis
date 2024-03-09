using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traditional.Api.Common.DataAccess.Entities;
using Traditional.Api.UseCases.Articles.Persistence.Entities;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

/// <summary>
/// Represents an attribute value associated with an <see cref="Article"/> and <see cref="Attribute"/>.
/// All attribute values with a defined type inherit from this abstract class.
/// </summary>
public abstract class AttributeValue : BaseEntity
{
    /// <summary>
    /// Gets or sets the id of the associated <see cref="Article"/>.
    /// </summary>
    [ForeignKey(nameof(Article))]
    public int ArticleId { get; set; }

    /// <summary>
    /// Gets or sets the id of the associated <see cref="Attribute"/>.
    /// </summary>
    [ForeignKey(nameof(Attribute))]
    public int AttributeId { get; set; }

    /// <summary>
    /// Gets or sets the associated <see cref="Article"/>.
    /// </summary>
    [Required]
    public Article? Article { get; set; }

    /// <summary>
    /// Gets or sets the associated <see cref="Attribute"/>.
    /// </summary>
    [Required]
    public Attribute? Attribute { get; set; }
}
