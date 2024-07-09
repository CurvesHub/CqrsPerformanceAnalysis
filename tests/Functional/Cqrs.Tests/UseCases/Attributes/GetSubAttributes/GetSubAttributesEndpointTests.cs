using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Cqrs.Api.UseCases.Attributes.GetSubAttributes;
using Cqrs.Tests.TestCommon.BaseTest;
using Cqrs.Tests.TestCommon.ErrorHandling;
using Cqrs.Tests.TestCommon.Factories;
using Cqrs.Tests.UseCases.Attributes.Common;
using ErrorOr;
using FluentAssertions;
using TestCommon.Constants;
using TestCommon.ErrorHandling;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Tests.UseCases.Attributes.GetSubAttributes;

public class GetSubAttributesEndpointTests(CqrsApiFactory factory)
    : BaseTestWithSharedCqrsApiFactory(factory)
{
    private GetSubAttributesRequest _getSubAttributesRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER,
        TestConstants.Attribute.ATTRIBUTE_IDS);

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData(AttributeValueType.Boolean)]
    [InlineData(AttributeValueType.Int)]
    [InlineData(AttributeValueType.Decimal)]
    [InlineData(AttributeValueType.String)]
    public async Task GetSubAttributesAsync_WhenAttributeIsRequiredButHasOptionalParent_ShouldReturnMinValuesZero(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        attribute.AttributeBooleanValues = [new AttributeBooleanValue(true) { Article = article }];
        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType, true).Single();
        var subSubAttribute = AttributeFactory.AddSubAttributesTo(subAttribute, 1, attributeValueType).Single();

        await dbContext.Attributes.AddRangeAsync(subAttribute, subSubAttribute);
        await dbContext.SaveChangesAsync();

        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = subSubAttribute.Id.ToString(CultureInfo.InvariantCulture) };

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(1);
        responses[0].MinValues.Should().Be(0);
    }

    [Fact]
    [Description("The product Type has an attribute with material as subAttribute, that usually is filtered out, due to mapping. " +
                 "e.g. PRODUCT,material,value should be filtered but PRODUCT,grip,material,value should not be filtered. ")]
    public async Task GetSubAttributesAsync_WhenMaterialIsFilteredOutButGripMaterialNot_ShouldReturnSubAttributes()
    {
        // Arrange
        const string material = "material";

        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        attribute.AttributeBooleanValues = [new AttributeBooleanValue(true) { Article = article }];

        var materialAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, AttributeValueType.String).Single();
        materialAttribute.Name = material;
        materialAttribute.MarketplaceAttributeIds = attribute.Name + "," + material;
        AttributeFactory.AddSubAttributesTo(materialAttribute, 1, AttributeValueType.String);

        var gripAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, AttributeValueType.String).Single();
        gripAttribute.Name = "grip";
        gripAttribute.MarketplaceAttributeIds = attribute.Name + ",grip";

        var gripMaterialDbAttribute = AttributeFactory.AddSubAttributesTo(gripAttribute, 1, AttributeValueType.String).Single();
        gripMaterialDbAttribute.Name = material;
        gripMaterialDbAttribute.MarketplaceAttributeIds = gripAttribute.MarketplaceAttributeIds + "," + material;

        var gripMaterialSubAttributes = AttributeFactory.AddSubAttributesTo(gripMaterialDbAttribute, 1, AttributeValueType.String).ToList();

        await dbContext.SaveChangesAsync();

        // Act
        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = attribute.Id.ToString(CultureInfo.InvariantCulture) };
        var productTypeResponses = await GetAttributesResponseAsync();

        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = gripAttribute.Id.ToString(CultureInfo.InvariantCulture) };
        var gripResponses = await GetAttributesResponseAsync();

        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = gripMaterialDbAttribute.Id.ToString(CultureInfo.InvariantCulture) };
        var gripMaterialResponses = await GetAttributesResponseAsync();

        // Assert
        productTypeResponses.Should().HaveCount(1);
        var productTypeResponse = productTypeResponses[0];

        List<Attribute> subAttributes = [gripAttribute];
        productTypeResponse.SubAttributes.Should().HaveCount(subAttributes.Count);
        productTypeResponse.SubAttributes.Should().BeEquivalentTo(subAttributes.Select(dbAttribute => dbAttribute.Id.ToString(CultureInfo.InvariantCulture)));
        productTypeResponse.SubAttributes.Should().NotContain(materialAttribute.Id.ToString(CultureInfo.InvariantCulture));

        gripResponses.Should().HaveCount(1);
        var gripResponse = gripResponses[0];
        List<Attribute> subAttributes1 = [gripMaterialDbAttribute];
        gripResponse.SubAttributes.Should().HaveCount(subAttributes1.Count);
        gripResponse.SubAttributes.Should().BeEquivalentTo(subAttributes1.Select(dbAttribute => dbAttribute.Id.ToString(CultureInfo.InvariantCulture)));

        gripMaterialResponses.Should().HaveCount(1);
        var gripMaterialResponse = gripMaterialResponses[0];
        gripMaterialResponse.SubAttributes.Should().HaveCount(gripMaterialSubAttributes.Count);
        gripMaterialResponse.SubAttributes.Should().BeEquivalentTo(gripMaterialSubAttributes.Select(dbAttribute => dbAttribute.Id.ToString(CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(AttributeValueType.Boolean)]
    [InlineData(AttributeValueType.Int)]
    [InlineData(AttributeValueType.Decimal)]
    [InlineData(AttributeValueType.String)]
    public async Task GetSubAttributesAsync_WhenAttributeHasSubAttributesWithValuesForDifferentVariants_ShouldReturnResponseWithValues(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (category, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 2, attributeValueType).ToList();

        var article1Values = Enumerable.Range(0, 5).Select(_ => AttributeFactory.CreateAttributeValue(attributeValueType)).ToList();
        article1Values.ForEach(value => AttributeFactory.AddValueToAttribute(subAttributes[0], value, article));
        article1Values.ForEach(value => AttributeFactory.AddValueToAttribute(subAttributes[1], value, article));

        var article2 = ArticleFactory.CreateArticle(characteristicId: 1, categories: [category]);
        var article2Values = Enumerable.Range(0, 5).Select(_ => AttributeFactory.CreateAttributeValue(attributeValueType)).ToList();
        article2Values.ForEach(value => AttributeFactory.AddValueToAttribute(subAttributes[0], value, article2));
        article2Values.ForEach(value => AttributeFactory.AddValueToAttribute(subAttributes[1], value, article2));

        await dbContext.Articles.AddAsync(article2);
        await dbContext.Attributes.AddRangeAsync(subAttributes);
        await dbContext.SaveChangesAsync();

        // Act
        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = attribute.Id.ToString(CultureInfo.InvariantCulture) };
        var responses1 = await GetAttributesResponseAsync();

        responses1.Should().HaveCount(1);
        responses1[0].SubAttributes.Should().NotBeNullOrEmpty();

        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = string.Join(',', responses1[0].SubAttributes!) };
        var responses2 = await GetAttributesResponseAsync();

        // Assert
        var responseSubAttribute1 = Array.Find(responses2, response => response.AttributeId == subAttributes[0].Id);
        var responseSubAttribute2 = Array.Find(responses2, response => response.AttributeId == subAttributes[1].Id);

        responseSubAttribute1.Should().NotBeNull();
        responseSubAttribute2.Should().NotBeNull();

        foreach (var article1Value in article1Values)
        {
            if (attributeValueType is AttributeValueType.Decimal)
            {
                var valueToCheck = article1Value + "0";
                responseSubAttribute1!.Values
                    .Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId
                        && innerValue.Values.Contains(valueToCheck, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                responseSubAttribute2!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId
                        && innerValue.Values.Contains(valueToCheck, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
            else
            {
                responseSubAttribute1!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId
                        && innerValue.Values.Contains(article1Value, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                responseSubAttribute2!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId
                        && innerValue.Values.Contains(article1Value, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
        }

        foreach (var article2Value in article2Values)
        {
            if (attributeValueType is AttributeValueType.Decimal)
            {
                var valueToCheck = article2Value + "0";
                responseSubAttribute1!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId + 1
                        && innerValue.Values.Contains(valueToCheck, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                responseSubAttribute2!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId + 1
                        && innerValue.Values.Contains(valueToCheck, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
            else
            {
                responseSubAttribute1!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId + 1
                        && innerValue.Values.Contains(article2Value, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                responseSubAttribute2!.Values.Exists(innerValue =>
                        innerValue.CharacteristicId == article.CharacteristicId + 1
                        && innerValue.Values.Contains(article2Value, StringComparer.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
        }
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task GetSubAttributesAsync_WhenArticleDoesNotExist_ShouldReturnArticleNotFoundError()
    {
        // Arrange
        _getSubAttributesRequest = _getSubAttributesRequest with { ArticleNumber = "99" };

        var expectedError = ArticleErrors.ArticleNotFound(_getSubAttributesRequest.ArticleNumber);

        // Act
        var response = await GetSubAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetSubAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GetSubAttributesAsync_WhenMappedCategoryDoesNotExist_ShouldReturnMappedCategoriesForArticleNotFound()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();

        var article = ArticleFactory.CreateArticle();
        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        var expectedError = ArticleErrors.MappedCategoriesForArticleNotFound(_getSubAttributesRequest.ArticleNumber, _getSubAttributesRequest.RootCategoryId);

        // Act
        var response = await GetSubAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetSubAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GetSubAttributesAsync_WhenAttributeIdIsUnknown_ShouldReturnUnknownAttributeIdsNotFoundError()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        await AttributeTestData.CreateTestData(dbContext);
        await dbContext.SaveChangesAsync();

        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = "99" };

        // Act
        var result = await GetSubAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetSubAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(result, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(AttributeErrors.AttributeIdsNotFound([99], _getSubAttributesRequest.RootCategoryId));
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData("abc")]
    [InlineData("a,b,c")]
    [InlineData("-1")]
    [InlineData("0.1")]
    [InlineData("-0.1")]
    [InlineData("1,-1")]
    [InlineData("1,2.1")]
    [InlineData("1.0")]
    public async Task GetSubAttributesAsync_WhenRequestIsNotValid_ShouldReturnValidationError(string attributeIds)
    {
        // Arrange
        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = attributeIds };

        var expectedError = Error.Validation(
            code: "AttributeIds",
            description: "The value of 'Attribute Ids' must be integers separated by comma and each must be greater than '0'.");

        // Act
        var response = await GetSubAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetSubAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1,2,3")]
    public async Task GetSubAttributesAsync_WhenRequestIsValid_ShouldNotReturnValidationError(string attributeIds)
    {
        // Arrange
        _getSubAttributesRequest = _getSubAttributesRequest with { AttributeIds = attributeIds };

        var expectedError = ArticleErrors.ArticleNotFound(_getSubAttributesRequest.ArticleNumber);

        // Act
        var response = await GetSubAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetSubAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    private async Task<GetAttributesResponse[]> GetAttributesResponseAsync()
    {
        var response = await GetSubAttributesAsync();
        response.EnsureSuccessStatusCode();

        var getAttributesResponses = await response.Content.ReadFromJsonAsync<GetAttributesResponse[]>();
        getAttributesResponses.Should().NotBeNull();

        return getAttributesResponses!;
    }

    private async Task<HttpResponseMessage> GetSubAttributesAsync()
    {
        return await HttpClient.GetAsync(CreateRequestUri());

        string CreateRequestUri()
        {
            var uriBuilder = new UriBuilder(HttpClient.BaseAddress!)
            {
                Path = EndpointRoutes.Attributes.GET_SUB_ATTRIBUTES
            };

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["rootCategoryId"] = _getSubAttributesRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            query["articleNumber"] = _getSubAttributesRequest.ArticleNumber;
            query["attributeIds"] = _getSubAttributesRequest.AttributeIds;

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.PathAndQuery;
        }
    }
}
