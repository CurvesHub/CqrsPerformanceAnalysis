using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.ErrorHandling;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.Attributes.Common.Errors;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Traditional.Api.UseCases.Attributes.Common.Responses;
using Traditional.Api.UseCases.Attributes.GetLeafAttributes;
using Traditional.Tests.TestCommon.BaseTest;
using Traditional.Tests.TestCommon.ErrorHandling;
using Traditional.Tests.TestCommon.Factories;
using Traditional.Tests.UseCases.Attributes.Common;

namespace Traditional.Tests.UseCases.Attributes.GetLeafAttributes;

public class GetLeafAttributesEndpointTests(TraditionalApiFactory factory)
    : BaseTestWithSharedTraditionalApiFactory(factory)
{
    private GetLeafAttributesRequest _getLeafAttributesRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER,
        TestConstants.Attribute.ATTRIBUTE_ID);

    // ReSharper disable once UseCollectionExpression - Not possible to use collection initializer with TheoryData
    public static TheoryData<AttributeValueType> AttributeValueTypes => new()
    {
        AttributeValueType.Boolean,
        AttributeValueType.Int,
        AttributeValueType.Decimal,
        AttributeValueType.String
    };

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task GetLeafAttributesAsync_WhenCalled_ShouldReturnLeafAttributesWithCorrectPaths(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, attribute, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 2, attributeValueType);
        await dbContext.SaveChangesAsync();

        _getLeafAttributesRequest = _getLeafAttributesRequest with { AttributeId = attribute.Id.ToString(CultureInfo.InvariantCulture) };

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        foreach (var leafAttribute in subAttribute.SubAttributes)
        {
            responses.Should().Contain(response =>
                response.AttributeId == leafAttribute.Id &&
                response.AttributePath![0] == subAttribute.Name);
        }
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task GetLeafAttributesAsync_WhenOptionalSubAttributeHasLeafs_ShouldReturnLeafsAsOptionalButDependent(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, _, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.MinValues = 0;
        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 3, attributeValueType);

        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        foreach (var leaf in subAttribute.SubAttributes)
        {
            var otherLeafIds = subAttribute.SubAttributes
                .Except([leaf])
                .Select(a => a.Id)
                .ToList();

            responses.Should().Contain(response =>
                response.AttributeId == leaf.Id && response.MinValues == 0 &&
                response.DependentAttributes.Intersect(otherLeafIds).Take(otherLeafIds.Count + 1).Count() == otherLeafIds.Count);
        }
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task GetLeafAttributesAsync_WhenLeafAttributeHasMaxValuesMinus1_ShouldReturnMaxValuesOfParentAttribute(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, _, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.MaxValues = 10;
        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 2, attributeValueType);

        var firstLeafAttribute = subAttribute.SubAttributes[0];
        firstLeafAttribute.MaxValues = -1;

        var lastLeafAttribute = subAttribute.SubAttributes[^1];
        lastLeafAttribute.MaxValues = 5;

        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().Contain(response => response.AttributeId == firstLeafAttribute.Id && response.MaxValues == 10);
        responses.Should().Contain(response => response.AttributeId == lastLeafAttribute.Id && response.MaxValues == 5);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task GetLeafAttributesAsync_WhenSubAttributeTreeIsMultipleLayersDeep_ShouldReturnAllLeafsWithCorrectDependentsAndPaths(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, _, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.MinValues = 0;
        var branches = AttributeFactory.AddSubAttributesTo(subAttribute, 3, attributeValueType);

        branches[0].MinValues = 0;
        branches.ForEach(attribute => AttributeFactory.AddSubAttributesTo(attribute, 3, attributeValueType)[0].MinValues = 0);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        foreach (var branch in branches)
        {
            foreach (var leaf in branch.SubAttributes!)
            {
                var dependentIds = branch.SubAttributes!
                    .Select(attribute => attribute.Id)
                    .Where(i => i != leaf.Id)
                    .ToList();

                responses.Should().Contain(response =>
                    response.AttributeId == leaf.Id &&
                    response.DependentAttributes.Intersect(dependentIds).Take(dependentIds.Count + 1).Count() == dependentIds.Count);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task GetLeafAttributesAsync_WhenAttributeHasSubAttributesWithDifferentDepth_ShouldReturnLeafAttributesOfAllLayers(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, _, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 2, attributeValueType);
        AttributeFactory.AddSubAttributesTo(subAttribute.SubAttributes[0], 3, attributeValueType);

        await dbContext.SaveChangesAsync();

        var leafIds = subAttribute.SubAttributes[0].SubAttributes!.Select(attribute => attribute.Id).Append(subAttribute.SubAttributes[1].Id).ToList();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        leafIds.ForEach(id => responses.Should().Contain(response => response.AttributeId == id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetLeafAttributesAsync_WhenAttributeHasSubAttributes_ShouldReturnColorIfArticleIsVariant(bool isVariant)
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, article, attribute, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 1, AttributeValueType.String);

        subAttribute.MarketplaceAttributeIds = attribute.MarketplaceAttributeIds + ",color";
        subAttribute.SubAttributes.Single().MarketplaceAttributeIds = subAttribute.MarketplaceAttributeIds + ",value";

        article.CharacteristicId = isVariant ? 1 : 0;

        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        if (isVariant)
        {
            responses.Should().Contain(response =>
                response.AttributeId == subAttribute.SubAttributes.Single().Id &&
                response.AttributePath![0] == subAttribute.Name);
        }
        else
        {
            responses.Should().NotContain(response =>
                response.AttributeId == subAttribute.SubAttributes.Single().Id &&
                response.AttributePath![0] == subAttribute.Name);
        }
    }

    [Fact]
    public async Task GetLeafAttributesAsync_WhenProductTypeIsNotSetAndHasSimilarSubAttributesToSetProductType_ShouldReturnValuesOfSetProductType()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var (category, article, _, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 2, AttributeValueType.String);

        var value1 = AttributeFactory.CreateAttributeValue(AttributeValueType.String);
        var value2 = AttributeFactory.CreateAttributeValue(AttributeValueType.String);

        var setAttribute = AttributeFactory.CreateAttribute(
            name: "SET",
            valueType: AttributeValueType.Boolean,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "SET" + AttributeValueType.Boolean + 1,
            rootCategory: germanRootCategory);

        var setSubAttribute = AttributeFactory.AddSubAttributesTo(setAttribute, 1, AttributeValueType.Boolean)[0];
        var setLeafAttributes = AttributeFactory.AddSubAttributesTo(setSubAttribute, 2, AttributeValueType.String);

        AttributeFactory.AddValueToAttribute(setAttribute, true.ToString(), article);
        AttributeFactory.AddValueToAttribute(setLeafAttributes[0], value1, article);
        AttributeFactory.AddValueToAttribute(setLeafAttributes[^1], value2, article);

        category.Attributes!.Add(setAttribute);

        await dbContext.Attributes.AddAsync(setAttribute);
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().HaveCount(2);
        responses.Should().Contain(response => response.Values.Exists(value => value.Values.Contains(value1)));
        responses.Should().Contain(response => response.Values.Exists(value => value.Values.Contains(value2)));
    }

    [Fact]
    public async Task GetLeafAttributesAsync_WhenAttributeHasMapping_ShouldNotReturnAttributeOrAnyOfItsSubAttributes()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        var (_, _, _, subAttribute) = await AttributeTestData.CreateTestDataWithSubAndLeafAttributes(dbContext);

        subAttribute.SubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 2, AttributeValueType.String);

        await dbContext.AttributeMappings.AddAsync(new AttributeMapping(subAttribute.MarketplaceAttributeIds.Split(",")[^1]) { Id = 99 });
        await dbContext.SaveChangesAsync();

        // Act
        var responses = await GetAttributesResponseAsync();

        // Assert
        responses.Should().BeEmpty();
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task GetLeafAttributesAsync_WhenArticleDoesNotExist_ShouldReturnArticleNotFoundError()
    {
        // Arrange
        _getLeafAttributesRequest = _getLeafAttributesRequest with { ArticleNumber = "99" };

        var expectedError = ArticleErrors.ArticleNotFound(_getLeafAttributesRequest.ArticleNumber);

        // Act
        var response = await GetLeafAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetLeafAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GetLeafAttributesAsync_WhenMappedCategoryDoesNotExist_ShouldReturnMappedCategoriesForArticleNotFound()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();

        var article = ArticleFactory.CreateArticle();
        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        var expectedError = ArticleErrors.MappedCategoriesForArticleNotFound(_getLeafAttributesRequest.ArticleNumber, _getLeafAttributesRequest.RootCategoryId);

        // Act
        var response = await GetLeafAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetLeafAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GetLeafAttributesAsync_WhenAttributeIdIsUnknown_ShouldReturnUnknownAttributeIdsNotFoundError()
    {
        // Arrange
        await using var dbContext = ResolveTraditionalDbContext();
        await AttributeTestData.CreateTestData(dbContext);
        await dbContext.SaveChangesAsync();

        _getLeafAttributesRequest = _getLeafAttributesRequest with { AttributeId = "99" };

        // Act
        var result = await GetLeafAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetLeafAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(result, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(AttributeErrors.AttributeIdsNotFound([99], _getLeafAttributesRequest.RootCategoryId));
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0.1")]
    [InlineData("-0.1")]
    [InlineData("1.0")]
    public async Task GetLeafAttributesAsync_WhenRequestIsNotValid_ShouldReturnValidationError(string attributeId)
    {
        // Arrange
        _getLeafAttributesRequest = _getLeafAttributesRequest with { AttributeId = attributeId };

        var expectedError = Error.Validation(
            code: "AttributeId",
            description: "The value of 'Attribute Id' must be greater than '0'.");

        // Act
        var response = await GetLeafAttributesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetLeafAttributesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    private async Task<GetAttributesResponse[]> GetAttributesResponseAsync()
    {
        var response = await GetLeafAttributesAsync();
        response.EnsureSuccessStatusCode();

        var getAttributesResponses = await response.Content.ReadFromJsonAsync<GetAttributesResponse[]>();
        getAttributesResponses.Should().NotBeNull();

        return getAttributesResponses!;
    }

    private async Task<HttpResponseMessage> GetLeafAttributesAsync()
    {
        return await HttpClient.GetAsync(CreateRequestUri());

        string CreateRequestUri()
        {
            var uriBuilder = new UriBuilder(HttpClient.BaseAddress!)
            {
                Path = EndpointRoutes.Attributes.GET_LEAF_ATTRIBUTES
            };

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["rootCategoryId"] = _getLeafAttributesRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            query["articleNumber"] = _getLeafAttributesRequest.ArticleNumber;
            query["attributeId"] = _getLeafAttributesRequest.AttributeId;

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.PathAndQuery;
        }
    }
}
