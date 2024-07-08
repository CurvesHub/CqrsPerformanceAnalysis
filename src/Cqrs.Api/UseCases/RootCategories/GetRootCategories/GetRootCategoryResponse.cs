using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.RootCategories.GetRootCategories;

/// <summary>
/// Represents the response for a single root category.
/// </summary>
/// <param name="RootCategoryId">The associated id.</param>
/// <param name="LocaleCode">The associated locale code.</param>
/// <param name="IsDefaultRoot">A bool indicating whether this is the default root (default root has localCode == de_DE).</param>
[PublicAPI]
public record GetRootCategoryResponse(
    int RootCategoryId,
    LocaleCode LocaleCode,
    bool IsDefaultRoot = false);
