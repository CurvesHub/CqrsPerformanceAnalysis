using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Categories.GetCategoryMapping;
using Cqrs.Api.UseCases.RootCategories.Common.Errors;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Cqrs.Tests.TestCommon.BaseTest;
using Cqrs.Tests.TestCommon.ErrorHandling;
using Cqrs.Tests.TestCommon.Factories;
using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.ErrorHandling;

namespace Cqrs.Tests.UseCases.Categories.GetCategoryMapping;

public class GetCategoryMappingEndpointTests(CqrsApiFactory factory)
    : BaseTestWithSharedCqrsApiFactory(factory)
{
    public record GetCategoryTestData(bool IsGermanRequest, bool WithEnglishCategory, GetCategoryMappingResponse ExpectedResponse);

    // ReSharper disable once UseCollectionExpression
    public static readonly TheoryData<GetCategoryTestData> SpecificScenarios = new()
    {
        // Test case 1: German request and article has only German category.
        // Result: Should return German category with German default values.
        new GetCategoryTestData(
            IsGermanRequest: true,
            WithEnglishCategory: false,
            ExpectedResponse: new GetCategoryMappingResponse(
                CategoryNumber: 1,
                CategoryPath: "Garten",
                GermanCategoryNumber: 1,
                GermanCategoryPath: "Garten")),
        // Test case 2: German request and article has German + English category.
        // Result: Should return German category with German default values.
        new GetCategoryTestData(
            IsGermanRequest: true,
            WithEnglishCategory: true,
            ExpectedResponse: new GetCategoryMappingResponse(
                CategoryNumber: 1,
                CategoryPath: "Garten",
                GermanCategoryNumber: 1,
                GermanCategoryPath: "Garten")),
        // Test case 3: English request and article has only German category.
        // Result: Should return empty category with German default values.
        new GetCategoryTestData(
            IsGermanRequest: false,
            WithEnglishCategory: false,
            ExpectedResponse: new GetCategoryMappingResponse(
                CategoryNumber: null,
                CategoryPath: null,
                GermanCategoryNumber: 1,
                GermanCategoryPath: "Garten")),
        // Test case 4: English request and article has German + English category.
        // Result: Should return the English category with German default values.
        new GetCategoryTestData(
            IsGermanRequest: false,
            WithEnglishCategory: true,
            ExpectedResponse: new GetCategoryMappingResponse(
                CategoryNumber: 1,
                CategoryPath: "Garden",
                GermanCategoryNumber: 1,
                GermanCategoryPath: "Garten"))
    };

    private BaseRequest _baseRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER);

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [MemberData(nameof(SpecificScenarios))]
    public async Task GetCategoryAsync_WhenSpecificScenarioOccurs_ShouldReturnExpectedResponse(GetCategoryTestData testData)
    {
        // Arrange
        var articleNumber = await AddArticleWithCategories(testData.WithEnglishCategory);

        _baseRequest = testData.IsGermanRequest
            ? new BaseRequest(TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID, articleNumber)
            : new BaseRequest(TestConstants.RootCategory.ENGLISH_ROOT_CATEGORY_ID, articleNumber);

        // Act
        var response = await GetCategoryMappingResponseAsync();

        // Assert
        response.Should().BeEquivalentTo(testData.ExpectedResponse);
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task GetCategoryAsync_WhenArticleDoesNotExist_ShouldReturnArticleNotFoundError()
    {
        // Arrange
        _baseRequest = _baseRequest with { ArticleNumber = "99" };

        var expectedError = ArticleErrors.ArticleNotFound(_baseRequest.ArticleNumber);

        // Act
        var response = await GetCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GetCategoryAsync_WhenMappedCategoryDoesNotExist_ShouldReturnMappedCategoriesForArticleNotFound()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();

        var article = ArticleFactory.CreateArticle();
        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        var expectedError = ArticleErrors.MappedCategoriesForArticleNotFound(_baseRequest.ArticleNumber, _baseRequest.RootCategoryId);

        // Act
        var response = await GetCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData(-1, "-1")]
    [InlineData(0, "0")]
    [InlineData(0, "abc")]
    public async Task GetCategoryAsync_WhenRequestIsNotValid_ShouldReturnValidationError(int rootCategoryId, string articleNumber)
    {
        // Arrange
        _baseRequest = new BaseRequest(rootCategoryId, articleNumber);

        Error[] expectedErrors =
        [
            Error.Validation(
                code: "RootCategoryId",
                description: "The value of 'Root Category Id' must be greater than '0'."),
            Error.Validation(
                code: "ArticleNumber",
                description: "The value of 'Article Number' must be greater than '0'.")
        ];

        // Act
        var response = await GetCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldBeEquivalentTo(expectedErrors);
    }

    [Fact]
    public async Task GetCategoryAsync_WhenRequestRootCategoryIsValidButDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        _baseRequest = _baseRequest with { RootCategoryId = 99 };

        var expectedError = RootCategoryErrors.RootCategoryIdNotFound(_baseRequest.RootCategoryId);

        // Act
        var response = await GetCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    private async Task<string> AddArticleWithCategories(bool withEnglishCategory)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();

        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.LocaleCode == LocaleCode.de_DE);
        var germanCategory = CategoryFactory.CreateCategory(1, path: "Garten", rootCategory: germanRootCategory);

        var article = ArticleFactory.CreateArticle(categories: [germanCategory]);

        if (withEnglishCategory)
        {
            var englishRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.LocaleCode == LocaleCode.en_GB);
            var englishCategory = CategoryFactory.CreateCategory(1, path: "Garden", rootCategory: englishRootCategory);
            article.Categories!.Add(englishCategory);
        }

        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        return article.ArticleNumber;
    }

    private async Task<GetCategoryMappingResponse> GetCategoryMappingResponseAsync()
    {
        var response = await GetCategoryMappingAsync();
        response.EnsureSuccessStatusCode();

        var getCategoryResponse = await response.Content.ReadFromJsonAsync<GetCategoryMappingResponse>();
        getCategoryResponse.Should().NotBeNull();

        return getCategoryResponse!;
    }

    private async Task<HttpResponseMessage> GetCategoryMappingAsync()
    {
        return await HttpClient.GetAsync(CreateRequestUri());

        string CreateRequestUri()
        {
            var uriBuilder = new UriBuilder(HttpClient.BaseAddress!)
            {
                Path = EndpointRoutes.Categories.GET_CATEGORY_MAPPING
            };

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["rootCategoryId"] = _baseRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            query["articleNumber"] = _baseRequest.ArticleNumber;

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.PathAndQuery;
        }
    }
}
