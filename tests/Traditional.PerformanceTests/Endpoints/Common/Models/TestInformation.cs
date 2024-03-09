using TestCommon.Constants;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.UseCases.Attributes.Common.Responses;
using Traditional.Api.UseCases.Attributes.GetLeafAttributes;
using Traditional.Api.UseCases.Attributes.GetSubAttributes;
using Traditional.Api.UseCases.Attributes.UpdateAttributeValues;
using Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;
using Traditional.Api.UseCases.Categories.SearchCategories;
using Traditional.Api.UseCases.Categories.UpdateCategoryMapping;

namespace Traditional.PerformanceTests.Endpoints.Common.Models;

/// <summary>
/// Provides information for testing an endpoint.
/// </summary>
public record TestInformation
{
    /// <summary>
    /// Gets the name of the endpoint.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    /// Gets the name of the directory where the scripts and result folders are located.
    /// </summary>
    public string TestDirectoryName { get; }

    /// <summary>
    /// Gets the route of the endpoint.
    /// </summary>
    public string EndpointRoute { get; }

    /// <summary>
    /// Gets the request to send to the endpoint.
    /// </summary>
    public object? WarmUpRequest { get; }

    /// <summary>
    /// Gets a value indicating whether to check if the elastic search container is running.
    /// </summary>
    public bool CheckElastic { get; }

    /// <summary>
    /// Gets a value indicating whether to send a warm-up request to the endpoint.
    /// </summary>
    public bool WithWarmUp { get; }

    /// <summary>
    /// Gets a value indicating whether to save only minimal results.
    /// </summary>
    public bool SaveMinimalResults { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestInformation"/> class.
    /// </summary>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <param name="testDirectoryName">The name of the test directory.</param>
    /// <param name="endpointRoute">The route of the endpoint.</param>
    /// <param name="warmUpRequest">The request to send to the endpoint.</param>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">The request object to send to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
#pragma warning disable S107 // Constructors should not have too many parameters - Needed for the test handler
    private TestInformation(
        string endpointName,
        string testDirectoryName,
        string endpointRoute,
        object? warmUpRequest,
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        EndpointName = endpointName;
        TestDirectoryName = testDirectoryName;
        EndpointRoute = endpointRoute;
        WarmUpRequest = warmUpRequest;
        CheckElastic = checkElastic;
        WithWarmUp = withWarmUp;
        SaveMinimalResults = saveMinimalResults;
    }
#pragma warning restore S107

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the GetAttributes endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForGetAttributes(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "GetAttributes",
            testDirectoryName: "Attributes",
            endpointRoute: EndpointRoutes.Attributes.GET_ATTRIBUTES,
            warmUpRequest: new BaseRequest(RootCategoryId: 1, ArticleNumber: "1"),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the GetLeafAttributes endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForGetLeafAttributes(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "GetLeafAttributes",
            testDirectoryName: "Attributes",
            endpointRoute: EndpointRoutes.Attributes.GET_LEAF_ATTRIBUTES,
            warmUpRequest: new GetLeafAttributesRequest(RootCategoryId: 1, ArticleNumber: "1", AttributeId: "1"),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the GetSubAttributes endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForGetSubAttributes(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "GetSubAttributes",
            testDirectoryName: "Attributes",
            endpointRoute: EndpointRoutes.Attributes.GET_SUB_ATTRIBUTES,
            warmUpRequest: new GetSubAttributesRequest(RootCategoryId: 1, ArticleNumber: "1", AttributeIds: "1"),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the UpdateAttributeValues endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForUpdateAttributeValues(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
#pragma warning disable SA1116, SA1118
        var request = new UpdateAttributeValuesRequest(
            RootCategoryId: 1,
            ArticleNumber: "1",
            [
                new NewAttributeValue(1,
                [
                    new VariantAttributeValues(0, ["True"])
                ]),
                new NewAttributeValue(30001,
                [
                    new VariantAttributeValues(0, ["True"])
                ]),
                new NewAttributeValue(60001,
                [
                    new VariantAttributeValues(0, ["True"])
                ])
            ]);
#pragma warning restore SA1116, SA1118

        return new TestInformation(
            endpointName: "UpdateAttributeValues",
            testDirectoryName: "Attributes",
            endpointRoute: EndpointRoutes.Attributes.UPDATE_ATTRIBUTE_VALUES,
            warmUpRequest: request,
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the GetCategoryMapping endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForGetCategoryMapping(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "GetCategoryMapping",
            testDirectoryName: "Categories",
            endpointRoute: EndpointRoutes.Categories.GET_CATEGORY_MAPPING,
            warmUpRequest: new BaseRequest(RootCategoryId: 1, ArticleNumber: "1"),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the GetChildrenOrTopLevel endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForGetChildrenOrTopLevel(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "GetChildrenOrTopLevel",
            testDirectoryName: "Categories",
            endpointRoute: EndpointRoutes.Categories.GET_CHILDREN_OR_TOP_LEVEL,
            warmUpRequest: new GetChildrenOrTopLevelRequest(RootCategoryId: 1, ArticleNumber: "1", CategoryNumber: 2),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the SearchCategories endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForSearchCategories(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "SearchCategories",
            testDirectoryName: "Categories",
            endpointRoute: EndpointRoutes.Categories.SEARCH_CATEGORIES,
            warmUpRequest: new SearchCategoriesRequest(RootCategoryId: 1, ArticleNumber: "1", CategoryNumber: 2),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the UpdateCategoryMapping endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForUpdateCategoryMapping(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "UpdateCategoryMapping",
            testDirectoryName: "Categories",
            endpointRoute: EndpointRoutes.Categories.UPDATE_CATEGORY_MAPPING,
            warmUpRequest: new UpdateCategoryMappingRequest(RootCategoryId: 1, ArticleNumber: "1", CategoryNumber: 2),
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestInformation"/> class for the GetRootCategories endpoint.
    /// </summary>
    /// <param name="checkElastic">Indicates whether to check if the elastic search container is running.</param>
    /// <param name="withWarmUp">Indicates whether to send a warm-up request to the endpoint.</param>
    /// <param name="saveMinimalResults">Indicates whether to save only minimal results.</param>
    /// <returns>A new instance of the <see cref="TestInformation"/> class.</returns>
    public static TestInformation CreateInfoForGetRootCategories(
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults)
    {
        return new TestInformation(
            endpointName: "GetRootCategories",
            testDirectoryName: "RootCategories",
            endpointRoute: EndpointRoutes.RootCategories.GET_ROOT_CATEGORIES,
            warmUpRequest: null,
            checkElastic: checkElastic,
            withWarmUp: withWarmUp,
            saveMinimalResults: saveMinimalResults);
    }
}
