using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using PerformanceTests.Common.Constants;
using TestCommon.Constants;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.UseCases.Attributes.GetLeafAttributes;
using Traditional.Api.UseCases.Attributes.GetSubAttributes;
using Traditional.Api.UseCases.Attributes.UpdateAttributeValues;
using Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;
using Traditional.Api.UseCases.Categories.SearchCategories;
using Traditional.Api.UseCases.Categories.UpdateCategoryMapping;
using ILogger = Serilog.ILogger;

namespace PerformanceTests.Common.Services;

/// <summary>
/// Provides a client to interact with the api.
/// </summary>
/// <param name="_logger">The logger.</param>
public class ApiClient(ILogger _logger)
{
    private readonly HttpClient _client = new();

    /// <summary>
    /// Waits for the api to be ready by sending a request to <see cref="EndpointRoutes.RootCategories.GET_ROOT_CATEGORIES"/>.
    /// </summary>
    /// <param name="apiToUse">The api to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the api is not ready after 10 seconds.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task WaitForApiToBeReady(AvailableApiNames apiToUse, CancellationToken cancellationToken = default)
    {
        _logger.Information("Waiting for the {ApiType} api to be ready", apiToUse);

        const int maxAttempts = 10;
        const int delay = 1000;
        await Task.Delay(delay * 3, cancellationToken);

        var client = new HttpClient
        {
            BaseAddress = new Uri(apiToUse is AvailableApiNames.TraditionalApi
                ? $"http://localhost:{AvailableApiPorts.TRADITIONAL_API_PORT}"
                : $"http://localhost:{AvailableApiPorts.CQRS_API_PORT}")
        };

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

                var response = await client.GetAsync(EndpointRoutes.RootCategories.GET_ROOT_CATEGORIES, cancellationToken);
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

    /// <summary>
    /// Sends ten warm up requests to the api.
    /// </summary>
    /// <param name="apiToUse">The api to use.</param>
    /// <param name="route">The route to send the requests to.</param>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the warm up request fails.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SendWarmupRequestsAsync(
        AvailableApiNames apiToUse,
        string route,
        object? request = null,
        CancellationToken cancellationToken = default)
    {
        const int requestCount = 10;

        _logger.Information("Sending {RequestCount} warm up requests", requestCount);

        _client.BaseAddress = new Uri(apiToUse is AvailableApiNames.TraditionalApi
            ? $"http://localhost:{AvailableApiPorts.TRADITIONAL_API_PORT}"
            : $"http://localhost:{AvailableApiPorts.CQRS_API_PORT}");

        const int delay = 250;

        for (var i = 0; i < requestCount; i++)
        {
            _logger.Information("Sending request {Attempt}/{Total}", i + 1, 10);

            await SendRequestBasedOnType(route, request, cancellationToken);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task SendRequestBasedOnType(string route, object? request, CancellationToken cancellationToken)
    {
        var response = request switch
        {
            null => await _client.GetAsync(route, cancellationToken), // Root categories
            GetLeafAttributesRequest getLeafAttributesRequest =>
                await _client.GetAsync(CreateRequestUriForGetLeafAttributes(getLeafAttributesRequest), cancellationToken),
            GetSubAttributesRequest getSubAttributesRequest =>
                await _client.GetAsync(CreateRequestUriForGetSubAttributes(getSubAttributesRequest), cancellationToken),
            UpdateAttributeValuesRequest updateAttributeValuesRequest =>
                await _client.PutAsJsonAsync(route, updateAttributeValuesRequest, cancellationToken: cancellationToken),
            GetChildrenOrTopLevelRequest getChildrenOrTopLevelRequest =>
                await _client.GetAsync(CreateRequestUriForGetChildrenOrTopLevel(getChildrenOrTopLevelRequest), cancellationToken),
            SearchCategoriesRequest searchCategoriesRequest =>
                await _client.GetAsync(CreateRequestUriForSearchCategories(searchCategoriesRequest), cancellationToken),
            UpdateCategoryMappingRequest updateCategoryMappingRequest =>
                await _client.PutAsJsonAsync(route, updateCategoryMappingRequest, cancellationToken: cancellationToken),
            BaseRequest baseRequest =>
                await _client.GetAsync(CreateUriBuilderFromBaseRequest(baseRequest, route).Uri.PathAndQuery, cancellationToken),
            _ => throw new NotSupportedException("The request type is not supported.")
        };

        if (!response.IsSuccessStatusCode)
        {
            var requestName = request?.GetType().Name ?? "Root categories";
            throw new InvalidOperationException($"Warmup request '{requestName}' failed.");
        }
    }

    private string CreateRequestUriForSearchCategories(SearchCategoriesRequest request)
    {
        return CreateRequestUri(request, EndpointRoutes.Categories.SEARCH_CATEGORIES, query =>
        {
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

            return query;
        });
    }

    private string CreateRequestUriForGetChildrenOrTopLevel(GetChildrenOrTopLevelRequest request)
    {
        return CreateRequestUri(request, EndpointRoutes.Categories.GET_CHILDREN_OR_TOP_LEVEL, query =>
        {
            if (request.CategoryNumber is not null)
            {
                query["categoryNumber"] = request.CategoryNumber?.ToString(CultureInfo.InvariantCulture);
            }

            return query;
        });
    }

    private string CreateRequestUriForGetSubAttributes(GetSubAttributesRequest request)
    {
        return CreateRequestUri(request, EndpointRoutes.Attributes.GET_SUB_ATTRIBUTES, query =>
        {
            if (!string.IsNullOrWhiteSpace(request.AttributeIds))
            {
                query["attributeIds"] = request.AttributeIds;
            }

            return query;
        });
    }

    private string CreateRequestUriForGetLeafAttributes(GetLeafAttributesRequest request)
    {
        return CreateRequestUri(request, EndpointRoutes.Attributes.GET_LEAF_ATTRIBUTES, query =>
        {
            if (!string.IsNullOrWhiteSpace(request.AttributeId))
            {
                query["attributeId"] = request.AttributeId;
            }

            return query;
        });
    }

    private string CreateRequestUri(BaseRequest request, string route, Func<NameValueCollection, NameValueCollection> modifyQuery)
    {
        var uriBuilder = CreateUriBuilderFromBaseRequest(request, route);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query = modifyQuery(query);

        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri.PathAndQuery;
    }

    private UriBuilder CreateUriBuilderFromBaseRequest(BaseRequest baseRequest, string route)
    {
        var uriBuilder = new UriBuilder(_client.BaseAddress!)
        {
            Path = route
        };

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query["rootCategoryId"] = baseRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
        query["articleNumber"] = baseRequest.ArticleNumber;

        uriBuilder.Query = query.ToString();
        return uriBuilder;
    }
}
