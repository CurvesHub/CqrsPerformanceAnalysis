using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.ErrorHandling;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Traditional.Api.UseCases.Attributes.Common.Responses;
using Traditional.Api.UseCases.RootCategories.Common.Errors;
using Traditional.Tests.TestCommon.BaseTest;
using Traditional.Tests.TestCommon.ErrorHandling;
using Traditional.Tests.TestCommon.Factories;
using Traditional.Tests.UseCases.Attributes.Common;

namespace Traditional.Tests.UseCases.Attributes.GetAttributes;

public class GetAttributesEndpointTests(TraditionalApiFactory factory)
    : BaseTestWithSharedTraditionalApiFactory(factory)
{
    private BaseRequest _baseRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER);

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task GetAttributesAsync_WhenAttributeExists_ShouldReturnAttribute()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, attribute) = await AttributeTestData.CreateTestData(dbContext);

        attribute.ValueType = AttributeValueType.String;
        attribute.AllowedValues = "allowed1,allowed2";
        attribute.MinLength = 10;
        attribute.MaxLength = 100;
        attribute.IsEditable = false;
        attribute.ExampleValues = "example1,example2";
        attribute.Description = "description text";

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 3).ToList();

        await dbContext.Attributes.AddRangeAsync(subAttributes);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(1);
        var response = responses[0];

        response.AttributeId.Should().Be(attribute.Id);
        response.AttributeName.Should().Be(attribute.Name);
        response.MinValues.Should().Be(attribute.MinValues);
        response.MaxValues.Should().Be(attribute.MaxValues);
        response.AllowedValues.Should().BeEquivalentTo(attribute.AllowedValues!.Split(','));
        response.MinLength.Should().Be(attribute.MinLength);
        response.MaxLength.Should().Be(attribute.MaxLength);
        response.IsEditable.Should().Be(attribute.IsEditable);
        response.ExampleValues.Should().BeEquivalentTo(attribute.ExampleValues!.Split(','));
        response.Description.Should().Be(attribute.Description);
        response.Type.Should().Be(attribute.ValueType.ToString().ToUpper(CultureInfo.InvariantCulture));
        response.SubAttributes.Should().HaveCount(subAttributes.Count);
        response.SubAttributes.Should().BeEquivalentTo(subAttributes.Select(dbAttribute => dbAttribute.Id.ToString(CultureInfo.InvariantCulture)));
    }

    [Fact]
    public async Task GetAttributesAsync_WhenAttributeIsVariationThemeAndArticleHasVariants_ShouldReturnMinValuesGreaterThanZero()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (category, _, attribute) = await AttributeTestData.CreateTestData(dbContext);

        attribute.MarketplaceAttributeIds = "variation_theme";
        var variantArticle = ArticleFactory.CreateArticle(characteristicId: 1, categories: [category]);

        await dbContext.Articles.AddAsync(variantArticle);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(1);
        responses[0].MinValues.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetAttributesAsync_WhenAttributeHasSubAttributes_ShouldNotReturnBaseAttribute()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var additionalAttributes = AttributeFactory.AddSubAttributesTo(attribute, 3).ToArray();
        var subAttributes = additionalAttributes.Take(2).ToList();
        var baseAttribute = additionalAttributes[^1];

        var attributeMapping = new AttributeMapping(baseAttribute.MarketplaceAttributeIds.Split(",")[^1]) { Id = 99 };

        await dbContext.Attributes.AddRangeAsync(additionalAttributes);
        await dbContext.AttributeMappings.AddAsync(attributeMapping);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(1);
        responses[0].SubAttributes.Should().HaveCount(subAttributes.Count);
        responses[0].SubAttributes.Should().BeEquivalentTo(subAttributes.Select(internalAttribute => internalAttribute.Id.ToString(CultureInfo.InvariantCulture)));
        responses[0].SubAttributes.Should().NotContain(baseAttribute.Id.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task GetAttributesAsync_WhenVariantsHaveDifferentSetProductTypes_ShouldReturnProductTypeWithMostArticlesAsSet()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var (category, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var variants = ArticleFactory.CreateVariants(3, categories: [category]).ToArray();
        var variant1 = variants[0];
        var variant2 = variants[1];
        var variant3 = variants[2];

        attribute.AttributeBooleanValues = [new AttributeBooleanValue(true) { Article = variant1 }];

        var differentAttribute = AttributeFactory.CreateAttribute(
            valueType: AttributeValueType.Boolean,
            marketplaceAttributeIds: "different",
            rootCategory: germanRootCategory,
            categories: [category],
            attributeBooleanValues: [new AttributeBooleanValue(true) { Article = variant2 }]);

        await dbContext.Attributes.AddAsync(differentAttribute);
        await dbContext.Articles.AddRangeAsync(variant1, variant2, variant3);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(2);

        int[] characteristicIds = [article.CharacteristicId, variant1.CharacteristicId, variant2.CharacteristicId, variant3.CharacteristicId];
        ResponseShouldHaveValueForVariants(responses, attribute.Id, containsTrue: true, characteristicIds);
        ResponseShouldHaveValueForVariants(responses, differentAttribute.Id, containsTrue: false, characteristicIds);
    }

    [Fact]
    public async Task GetAttributesAsync_WhenVariantsHaveNoAssignedProductTypes_ShouldReturnNoProductTypeAsAssigned()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var (category, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var variants = ArticleFactory.CreateVariants(3, categories: [category]).ToArray();
        var variant1 = variants[0];
        var variant2 = variants[1];
        var variant3 = variants[2];

        var differentAttribute = AttributeFactory.CreateAttribute(
            marketplaceAttributeIds: "different",
            rootCategory: germanRootCategory,
            categories: [category]);

        await dbContext.Attributes.AddAsync(differentAttribute);
        await dbContext.Articles.AddRangeAsync(variant1, variant2, variant3);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(2);

        int[] characteristicIds = [article.CharacteristicId, variant1.CharacteristicId, variant2.CharacteristicId, variant3.CharacteristicId];
        ResponseShouldHaveValueForVariants(responses, attribute.Id, containsTrue: false, characteristicIds);
        ResponseShouldHaveValueForVariants(responses, differentAttribute.Id, containsTrue: false, characteristicIds);
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task GetAttributesAsync_WhenArticleDoesNotExist_ShouldReturnArticleNotFoundError()
    {
        // Arrange
        _baseRequest = _baseRequest with { ArticleNumber = "99" };

        var expectedError = ArticleErrors.ArticleNotFound(_baseRequest.ArticleNumber);

        // Act
        var response = await GetAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GetAttributesAsync_WhenMappedCategoryDoesNotExist_ShouldReturnMappedCategoriesForArticleNotFound()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();

        var article = ArticleFactory.CreateArticle();
        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        var expectedError = ArticleErrors.MappedCategoriesForArticleNotFound(_baseRequest.ArticleNumber, _baseRequest.RootCategoryId);

        // Act
        var response = await GetAttributesAsync();

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
    public async Task GetAttributesAsync_WhenRequestIsNotValid_ShouldReturnValidationError(int rootCategoryId, string articleNumber)
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
        var response = await GetAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldBeEquivalentTo(expectedErrors);
    }

    [Fact]
    public async Task GetAttributesAsync_WhenRequestRootCategoryIsValidButDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        _baseRequest = _baseRequest with { RootCategoryId = 99 };

        var expectedError = RootCategoryErrors.RootCategoryIdNotFound(_baseRequest.RootCategoryId);

        // Act
        var response = await GetAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<BaseRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    private static void ResponseShouldHaveValueForVariants(GetAttributesResponse[] responses, int attributeId, bool containsTrue, params int[] characteristicIds)
    {
        var response = responses.Should().ContainSingle(response => response.AttributeId == attributeId).Which;

        response.Values.Should().HaveCount(characteristicIds.Length).And.OnlyContain(attributeValue =>
            characteristicIds.Contains(attributeValue.CharacteristicId));

        response.Values.Should().OnlyContain(attributeValue =>
            containsTrue
                ? string.Equals(attributeValue.Values.Single(), "true", StringComparison.OrdinalIgnoreCase)
                : attributeValue.Values.Length == 0);
    }

    private async Task<GetAttributesResponse[]> GetAttributesResponseAsync()
    {
        var response = await GetAttributesAsync();
        response.EnsureSuccessStatusCode();

        var getAttributesResponses = await response.Content.ReadFromJsonAsync<GetAttributesResponse[]>();
        getAttributesResponses.Should().NotBeNull();

        return getAttributesResponses!;
    }

    private async Task<HttpResponseMessage> GetAttributesAsync()
    {
        return await HttpClient.GetAsync(CreateRequestUri());

        string CreateRequestUri()
        {
            var uriBuilder = new UriBuilder(HttpClient.BaseAddress!)
            {
                Path = EndpointRoutes.Attributes.GET_ATTRIBUTES
            };

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["rootCategoryId"] = _baseRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            query["articleNumber"] = _baseRequest.ArticleNumber;

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.PathAndQuery;
        }
    }
}
