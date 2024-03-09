using System.Globalization;
using System.Web;
using TestCommon.Constants;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.UseCases.Attributes.GetLeafAttributes;
using Traditional.Api.UseCases.Attributes.GetSubAttributes;
using Traditional.Api.UseCases.Attributes.UpdateAttributeValues;
using Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;
using Traditional.Api.UseCases.Categories.SearchCategories;
using Traditional.Api.UseCases.Categories.UpdateCategoryMapping;
using ILogger = Serilog.ILogger;

namespace Traditional.PerformanceTests.Endpoints.Common.Services;

/// <summary>
/// Provides a client to interact with the api.
/// </summary>
/// <param name="_logger">The logger.</param>
public class ApiClient(ILogger _logger)
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("http://localhost:5012") };

    /// <summary>
    /// Sends <paramref name="requestCount"/> warm up requests to the api.
    /// </summary>
    /// <param name="route">The route to send the requests to.</param>
    /// <param name="request">The request to send.</param>
    /// <param name="requestCount">The number of requests to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the warm up request fails.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SendWarmupRequestsAsync(
        string route,
        object? request = null,
        int requestCount = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Sending {RequestCount} warm up requests", requestCount);

        const int delay = 250;

        for (var i = 0; i < requestCount; i++)
        {
            _logger.Information("Sending request {Attempt}/{Total}", i + 1, 10);

            await SendRequestBasedOnType(route, request, cancellationToken);
            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <summary>
    /// Waits for the api to be ready by sending a request to '/metrics'.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the api is not ready after 10 seconds.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task WaitForApiToBeReady(CancellationToken cancellationToken = default)
    {
        _logger.Information("Waiting for the api to be ready");

        const int maxAttempts = 10;
        const int delay = 1000;
        await Task.Delay(delay * 3, cancellationToken);

        int exceptionCount = 0;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("The test was cancelled");
                return;
            }

            try
            {
                _logger.Information("Sending a request to '{EndpointRoute}'", EndpointRoutes.RootCategories.GET_ROOT_CATEGORIES);

                var response = await _client.GetAsync(EndpointRoutes.RootCategories.GET_ROOT_CATEGORIES, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.Information("The api is ready");
                    break;
                }
            }
            catch (HttpRequestException)
            {
                exceptionCount++;
                _logger.Information("The api is not ready yet! (HttpRequestException occurred)");
            }

            if (exceptionCount > maxAttempts)
            {
                _logger.Warning("The api is not ready after {MaxAttempts} seconds, killing the test", maxAttempts);
                throw new InvalidOperationException($"The api is not ready after {maxAttempts} seconds, the test was killed.");
            }

            _logger.Information("Waiting for another second");
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task SendRequestBasedOnType(string route, object? request, CancellationToken cancellationToken)
    {
        switch (request)
        {
            // Attributes
            case GetLeafAttributesRequest getLeafAttributesRequest:
                if (!(await _client.GetAsync(CreateRequestUriForGetLeafAttributes(getLeafAttributesRequest), cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'GetLeafAttributes' failed.");
                }

                break;
            case GetSubAttributesRequest getSubAttributesRequest:
                if (!(await _client.GetAsync(CreateRequestUriForGetSubAttributes(getSubAttributesRequest), cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'GetSubAttributes' failed.");
                }

                break;
            case UpdateAttributeValuesRequest updateAttributeValuesRequest:
                if (!(await _client.PutAsJsonAsync(route, updateAttributeValuesRequest, cancellationToken: cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'UpdateAttributeValues' failed.");
                }

                break;

            // Categories
            case GetChildrenOrTopLevelRequest getChildrenOrTopLevelRequest:
                if (!(await _client.GetAsync(CreateRequestUriForGetChildrenOrTopLevel(getChildrenOrTopLevelRequest), cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'GetChildrenOrTopLevel' failed.");
                }

                break;
            case SearchCategoriesRequest searchCategoriesRequest:
                if (!(await _client.GetAsync(CreateRequestUriForSearchCategories(searchCategoriesRequest), cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'SearchCategories' failed.");
                }

                break;
            case UpdateCategoryMappingRequest updateCategoryMappingRequest:
                if (!(await _client.PutAsJsonAsync(route, updateCategoryMappingRequest, cancellationToken: cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'UpdateCategoryMapping' failed.");
                }

                break;
            // Root categories
            case null:
                if (!(await _client.GetAsync(route, cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'GetRootCategories' failed.");
                }

                break;
            // Base request (GetAttributes, GetCategoryMapping)
            case BaseRequest baseRequest:
                if (string.Equals(route, EndpointRoutes.Attributes.GET_ATTRIBUTES, StringComparison.OrdinalIgnoreCase)
                    && !(await _client.GetAsync(CreateUriForBaseRequest(baseRequest, route), cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'GetAttributes' failed.");
                }

                if (string.Equals(route, EndpointRoutes.Categories.GET_CATEGORY_MAPPING, StringComparison.OrdinalIgnoreCase)
                    && !(await _client.GetAsync(CreateUriForBaseRequest(baseRequest, route), cancellationToken)).IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Warmup request 'GetCategoryMapping' failed.");
                }

                break;
            default:
                throw new NotSupportedException("The request type is not supported.");
        }
    }

    private string CreateRequestUriForSearchCategories(SearchCategoriesRequest request)
    {
        var uriBuilder = new UriBuilder(CreateUriFromBaseRequest(request))
        {
            Path = EndpointRoutes.Categories.SEARCH_CATEGORIES
        };

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (request.CategoryNumber is not null)
        {
            query.Remove("searchTerm");
            query["categoryNumber"] = request.CategoryNumber?.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            query.Remove("categoryNumber");
            query["searchTerm"] = request.SearchTerm;
        }

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri.PathAndQuery;
    }

    private string CreateRequestUriForGetChildrenOrTopLevel(GetChildrenOrTopLevelRequest request)
    {
        var uriBuilder = new UriBuilder(CreateUriFromBaseRequest(request))
        {
            Path = EndpointRoutes.Categories.GET_CHILDREN_OR_TOP_LEVEL
        };

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (request.CategoryNumber is not null)
        {
            query["categoryNumber"] = request.CategoryNumber?.ToString(CultureInfo.InvariantCulture);
        }

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri.PathAndQuery;
    }

    private string CreateRequestUriForGetSubAttributes(GetSubAttributesRequest request)
    {
        var uriBuilder = new UriBuilder(CreateUriFromBaseRequest(request))
        {
            Path = EndpointRoutes.Attributes.GET_SUB_ATTRIBUTES
        };

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrWhiteSpace(request.AttributeIds))
        {
            query["attributeIds"] = request.AttributeIds;
        }

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri.PathAndQuery;
    }

    private string CreateRequestUriForGetLeafAttributes(GetLeafAttributesRequest request)
    {
        var uriBuilder = new UriBuilder(CreateUriFromBaseRequest(request))
        {
            Path = EndpointRoutes.Attributes.GET_LEAF_ATTRIBUTES
        };

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrWhiteSpace(request.AttributeId))
        {
            query["attributeId"] = request.AttributeId;
        }

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri.PathAndQuery;
    }

    private Uri CreateUriFromBaseRequest(BaseRequest baseRequest)
    {
        var uriBuilder = new UriBuilder(_client.BaseAddress!);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query["rootCategoryId"] = baseRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
        query["articleNumber"] = baseRequest.ArticleNumber;

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri;
    }

    private Uri CreateUriForBaseRequest(BaseRequest baseRequest, string path)
    {
        var uriBuilder = new UriBuilder(_client.BaseAddress!)
        {
            Path = path
        };

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query["rootCategoryId"] = baseRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
        query["articleNumber"] = baseRequest.ArticleNumber;

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri;
    }
}
