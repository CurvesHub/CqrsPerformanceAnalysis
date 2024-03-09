using JetBrains.Annotations;

namespace Traditional.Api.Common.BaseRequests;

/// <summary>
/// Defines common properties for all requests.
/// </summary>
/// <param name="RootCategoryId">The requested root category id.</param>
/// <param name="ArticleNumber">The requested article number.</param>
[PublicAPI]
public record BaseRequest(int RootCategoryId, string ArticleNumber);
