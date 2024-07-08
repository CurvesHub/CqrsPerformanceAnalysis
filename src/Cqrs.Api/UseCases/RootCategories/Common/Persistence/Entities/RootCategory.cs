using Cqrs.Api.Common.DataAccess.Entities;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;

/// <summary>
/// Entity to store a root category.
/// </summary>
/// <param name="localeCode">Sets the associated <see cref="LocaleCode"/>.</param>
public class RootCategory(LocaleCode localeCode) : BaseEntity
{
    /// <summary>
    /// Gets the associated <see cref="LocaleCode"/>.
    /// </summary>
    public LocaleCode LocaleCode { get; init; } = localeCode;

    /// <summary>
    /// Gets a collection of the associated <see cref="Category"/>s.
    /// </summary>
    public List<Category>? Categories { get; init; }

    /// <summary>
    /// Gets a collection of the associated <see cref="Attribute"/>s.
    /// </summary>
    public List<Attribute>? Attributes { get; init; }
}
