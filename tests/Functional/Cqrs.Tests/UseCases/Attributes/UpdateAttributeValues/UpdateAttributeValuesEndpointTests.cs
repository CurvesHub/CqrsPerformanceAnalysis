using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Cqrs.Api.UseCases.Attributes.UpdateAttributeValues;
using Cqrs.Tests.TestCommon.BaseTest;
using Cqrs.Tests.TestCommon.ErrorHandling;
using Cqrs.Tests.TestCommon.Factories;
using Cqrs.Tests.UseCases.Attributes.Common;
using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.ErrorHandling;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Tests.UseCases.Attributes.UpdateAttributeValues;

public class UpdateAttributeValuesEndpointTests(CqrsApiFactory factory)
    : BaseTestWithSharedCqrsApiFactory(factory)
{
    private readonly List<NewAttributeValue> _newAttributeValues = [];

    private UpdateAttributeValuesRequest _updateAttributeValuesRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER,
        []);

    public record InvalidRequestWithExpectedError(IEnumerable<NewAttributeValue> NewAttributeValues, Error ExpectedError);

    // ReSharper disable once UseCollectionExpression - Not possible to use collection initializer with TheoryData
    public static TheoryData<AttributeValueType> AttributeValueTypes => new()
    {
        AttributeValueType.Boolean,
        AttributeValueType.Int,
        AttributeValueType.Decimal,
        AttributeValueType.String
    };

    // ReSharper disable once UseCollectionExpression - Not possible to use collection initializer with TheoryData
    public static TheoryData<AttributeValueType> AttributeValueTypesWithoutBoolean => new()
    {
        AttributeValueType.Int,
        AttributeValueType.Decimal,
        AttributeValueType.String
    };

    // ReSharper disable once UseCollectionExpression - Not possible to use collection initializer with TheoryData
    public static TheoryData<InvalidRequestWithExpectedError> InvalidRequestTestData => new()
    {
        new InvalidRequestWithExpectedError([], Error.Validation(code: "NewAttributeValues", description: "The value of 'New Attribute Values' must not be empty.")),
        new InvalidRequestWithExpectedError([new NewAttributeValue(0, [new VariantAttributeValues(0, ["True"])])], Error.Validation(code: "NewAttributeValues", description: "The value of 'New Attribute Values' -> 'Attribute Id' must be greater than '0'.")),
        new InvalidRequestWithExpectedError([new NewAttributeValue(1, [])], Error.Validation(code: "NewAttributeValues", description: "The value of 'New Attribute Values' -> 'Inner Values' must not be empty.")),
        new InvalidRequestWithExpectedError([new NewAttributeValue(1, [new VariantAttributeValues(-1, ["True"])])], Error.Validation(code: "NewAttributeValues", description: "The value of 'New Attribute Values' -> 'Inner Values' -> 'Characteristic Id' must be greater than or equal to '0'.")),
        new InvalidRequestWithExpectedError([new NewAttributeValue(1, [new VariantAttributeValues(0, [])])], Error.Validation(code: "NewAttributeValues", description: "The value of 'New Attribute Values' -> 'Inner Values' -> 'Values' must not be empty."))
    };

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenAttributeHasValidValues_ShouldSaveNewValues(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType, optional: true)[0];
        subAttribute.MaxLength = 100;
        subAttribute.MinLength = 0;
        subAttribute.MaxValues = 10;
        await dbContext.SaveChangesAsync();

        var attributeValues = Enumerable.Range(0, 10).Select(_ => AttributeFactory.CreateAttributeValue(attributeValueType)).ToList();

        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, string.Join(';', attributeValues));
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        subAttribute = attribute.SubAttributes!.Single();
        foreach (var attributeValue in attributeValues)
        {
            await AttributeInDbShouldHaveValue(subAttribute, attributeValue, article.Id);
        }
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenArticleHasDifferentProductTypeInDbAndRequest_ShouldDeleteAllValuesForAttributesOfOldProductType(AttributeValueType attributeValueType)
    {
        // Arrange
        int characteristicId, attributeId, subAttributeId, newProductTypeId, articleId;
        await using (var dbContext = ResolveCqrsWriteDbContext())
        {
            var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory =>
                rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
            var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

            var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType, optional: true)[0];

            AttributeFactory.AddValueToAttribute(attribute, "True", article);
            AttributeFactory.AddValueToAttribute(subAttribute, AttributeFactory.CreateAttributeValue(attributeValueType), article);

            var newProductType = AttributeFactory.CreateAttribute(
                name: "new",
                valueType: AttributeValueType.Boolean,
                minValues: 0,
                maxValues: 1,
                marketplaceAttributeIds: "new" + AttributeValueType.Boolean + 0,
                rootCategory: germanRootCategory);

            await dbContext.Attributes.AddAsync(newProductType);
            await dbContext.SaveChangesAsync();

            characteristicId = article.CharacteristicId;
            attributeId = attribute.Id;
            subAttributeId = subAttribute.Id;
            newProductTypeId = newProductType.Id;
            articleId = article.Id;
        }

        AddNewAttributeValues(newProductTypeId, characteristicId, "True");

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        await ArticleShouldHaveCountAttributeValues(articleId, 1, 0, 0, 0);
        await AttributeShouldHaveCountAttributeValues(attributeId, 0, 0, 0, 0);
        await AttributeShouldHaveCountAttributeValues(subAttributeId, 0, 0, 0, 0);
        await AttributeShouldHaveCountAttributeValues(newProductTypeId, 1, 0, 0, 0);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenValueInDbIsInRequest_ShouldUpdateValue(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType, optional: true)[0];
        AttributeFactory.AddValueToAttribute(subAttribute, AttributeFactory.CreateAttributeValue(attributeValueType), article);
        await dbContext.SaveChangesAsync();

        var newValue = AttributeFactory.CreateAttributeValue(attributeValueType);
        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, newValue);
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        subAttribute = attribute.SubAttributes!.Single();
        await AttributeInDbShouldHaveValue(subAttribute, newValue, article.Id);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenValuesForMultipleSubAttributesInRequest_ShouldSaveValues(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 1, AttributeValueType.Boolean);
        var subSubAttributes = AttributeFactory.AddSubAttributesTo(subAttributes[0], 10, attributeValueType);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        var values = CreateAndAddNewAttributeValuesForAttributes(attributeValueType, subSubAttributes, article);

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        subSubAttributes = attribute.SubAttributes!.Single().SubAttributes!;
        await AttributeInDbShouldHaveValue(attribute, "True", article.Id);
        await AttributesInDbShouldHaveValues(subSubAttributes, values, article.Id);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenDifferentValuesForMultipleArticles_ShouldSaveValues(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var (category, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var notSetProductType = AttributeFactory.CreateAttribute(
            name: "notSetProductType",
            valueType: AttributeValueType.Boolean,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "notSetProductType" + AttributeValueType.Boolean + 1,
            rootCategory: germanRootCategory);

        category.Attributes!.Add(notSetProductType);

        var article2 = ArticleFactory.CreateArticle(characteristicId: 1, categories: [category]);
        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 1, AttributeValueType.Boolean);
        var subSubAttributes = AttributeFactory.AddSubAttributesTo(subAttributes[0], 10, attributeValueType);

        await dbContext.Articles.AddAsync(article2);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(notSetProductType.Id, article.CharacteristicId, "False");
        var valueLists = CreateAndAddNewAttributeValuesForAttributes(attributeValueType, subSubAttributes, [article, article2]);

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        subSubAttributes = attribute.SubAttributes!.Single().SubAttributes!;

        await AttributeInDbShouldHaveValue(attribute, "True", article.Id);
        await AttributesInDbShouldHaveValues(subSubAttributes, valueLists[0], article.Id);
        await AttributesInDbShouldHaveValues(subSubAttributes, valueLists[1], article2.Id);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenValuesForMultipleSubAttributesInRequest_ShouldIgnoreValuesOfNoneProductTypeAttributesWithSubAttributes(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, AttributeValueType.Boolean)[0];
        var subSubAttributes = AttributeFactory.AddSubAttributesTo(subAttribute, 10, attributeValueType);

        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, subAttribute.Name);
        var values = CreateAndAddNewAttributeValuesForAttributes(attributeValueType, subSubAttributes, article);

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        subAttribute.AttributeBooleanValues.Should().BeNull();
        subSubAttributes = attribute.SubAttributes!.Single().SubAttributes!;
        await AttributeInDbShouldHaveValue(attribute, "True", article.Id);
        await AttributesInDbShouldHaveValues(subSubAttributes, values, article.Id);
    }

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenAttributeHasValuesOfOtherRootCategory_ShouldPreservedOtherValuesWhenSavingNewValues()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var article = ArticleFactory.CreateArticle();
        CreateHomeProductTypeOnDeTreeWithNumberOfItemsSet(article, dbContext);

        var (homeEnglish, materialValueEnglish) = CreateAttributesForEnglishRootCategory(article, dbContext);

        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(homeEnglish.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(materialValueEnglish.Id, article.CharacteristicId, "Other");

        _updateAttributeValuesRequest = _updateAttributeValuesRequest with { RootCategoryId = TestConstants.RootCategory.ENGLISH_ROOT_CATEGORY_ID };

        // Act
        await AssertStatusCodeForUpdateAttributeValuesAsync();

        // Assert
        await using var newDbContext = ResolveCqrsWriteDbContext();

        var productTypes = await newDbContext.Attributes
            .Where(a => a.ParentAttribute == null)
            .Include(a => a.ParentAttribute)
            .Include(a => a.SubAttributes)!
            .ThenInclude(a => a.SubAttributes)
            .Include(a => a.AttributeBooleanValues)
            .Include(a => a.AttributeDecimalValues)
            .Include(a => a.AttributeIntValues)
            .Include(a => a.AttributeStringValues)
            .ToListAsync();

        var germanHome = productTypes.Single(attribute => attribute.RootCategoryId == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var englishHome = productTypes.Single(attribute => attribute.RootCategoryId == TestConstants.RootCategory.ENGLISH_ROOT_CATEGORY_ID);

        // German values should be preserved
        await AttributeInDbShouldHaveValue(germanHome, "True", article.Id);
        await AttributeInDbShouldHaveValue(germanHome.SubAttributes!.Single().SubAttributes!.Single(), "1", article.Id);

        // English number of items value should be erased
        var numberOfItemsIt = englishHome.SubAttributes!.Single(a => string.Equals(a.MarketplaceAttributeIds, "HOME,number_of_items", StringComparison.Ordinal));
        var numberOfItemsValueIt = numberOfItemsIt.SubAttributes!.Single();
        await ArticleShouldNotHaveValueForAttribute(numberOfItemsValueIt.Id, article);

        // New English values should be set
        await AttributeInDbShouldHaveValue(englishHome, "True", article.Id);
        await AttributeInDbShouldHaveValue(
            englishHome.SubAttributes!.Single(a => string.Equals(a.MarketplaceAttributeIds, "HOME,material", StringComparison.Ordinal)).SubAttributes!.Single(),
            "Other",
            article.Id);
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenArticleDoesNotExist_ShouldReturnArticleNotFoundError()
    {
        // Arrange
        _updateAttributeValuesRequest = _updateAttributeValuesRequest with { ArticleNumber = "99" };

        var expectedError = ArticleErrors.ArticleNotFound(_updateAttributeValuesRequest.ArticleNumber);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenMappedCategoryDoesNotExist_ShouldReturnMappedCategoriesForArticleNotFound()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();

        var article = ArticleFactory.CreateArticle();
        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        var expectedError = ArticleErrors.MappedCategoriesForArticleNotFound(_updateAttributeValuesRequest.ArticleNumber, _updateAttributeValuesRequest.RootCategoryId);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenDuplicateAttributeId_ShouldReturnDuplicateAttributeIdError()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.DuplicateAttributeIds([attribute.Id]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenUnknownAttributeId_ShouldReturnUnknownAttributeIdError()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(attribute.Id + 10, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.AttributeIdsNotFound([attribute.Id + 10], null);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenNoProductTypeIsSet_ShouldReturnNotEnoughValuesError()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, string.Empty);

        var expectedError = AttributeErrors.NotEnoughValues(attribute.Id, 0, 1);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task UpdateAttributeValuesAsync_WhenMoreThanOneProductTypeIsSet_ShouldReturnTooManyValuesError()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var otherProductType = AttributeFactory.CreateAttribute(
            name: "otherProductType",
            valueType: AttributeValueType.Boolean,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "otherProductType" + AttributeValueType.Boolean + 1,
            rootCategory: germanRootCategory);

        await dbContext.Attributes.AddAsync(otherProductType);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(otherProductType.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.TooManyValues(attribute.Id, 2, 1);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenInvalidCharacteristicId_ShouldReturnCharacteristicIdNotFoundError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var otherProductType = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];

        await dbContext.SaveChangesAsync();

        var otherCharacteristicId = article.CharacteristicId + 10;

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(otherProductType.Id, otherCharacteristicId, string.Empty);

        var expectedError = AttributeErrors.CharacteristicIdNotFound(otherProductType.Id, [otherCharacteristicId]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenTooFewValuesForAttribute_ShouldReturnNotEnoughValuesError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];
        subAttribute.MinValues = 2;

        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(attributeValueType));
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.NotEnoughValues(subAttribute.Id, 1, 2);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenTooManyValuesForAttribute_ShouldReturnTooManyValuesError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];
        subAttribute.MaxValues = 1;
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(attributeValueType) + ";" + AttributeFactory.CreateAttributeValue(attributeValueType));
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.TooManyValues(subAttribute.Id, 2, 1);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenMissingRequiredAttribute_ShouldReturnRequiredAttributeMissingError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 2, attributeValueType);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(subAttributes[0].Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(subAttributes[0].ValueType));
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.RequiredAttributeMissing(
            attribute.Id,
            $"{article.ArticleNumber}_{article.CharacteristicId}",
            [subAttributes[1].Id]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenMissingRequiredAttributeForOneArticle_ShouldReturnAttributeMissingErrorWithSku(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (category, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 2, attributeValueType);
        var article2 = ArticleFactory.CreateArticle(characteristicId: 1, categories: [category]);

        await dbContext.Articles.AddAsync(article2);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, [article.CharacteristicId, article2.CharacteristicId], ["True", "True"]);
        AddNewAttributeValues(subAttributes[0].Id, [article.CharacteristicId, article2.CharacteristicId], [AttributeFactory.CreateAttributeValue(subAttributes[0].ValueType), AttributeFactory.CreateAttributeValue(subAttributes[0].ValueType)]);
        AddNewAttributeValues(subAttributes[1].Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(subAttributes[1].ValueType));

        var expectedError = AttributeErrors.RequiredAttributeMissing(
            attribute.Id,
            $"{article2.ArticleNumber}_{article2.CharacteristicId}",
            [subAttributes[1].Id]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenAttributeHasValueForOptionalSubAttributeButNotForRequiredSubAttribute_ShouldReturnAttributeMissingError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 2, attributeValueType);
        subAttributes.AddRange(AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType, optional: true));
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(subAttributes[0].Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(subAttributes[0].ValueType));
        AddNewAttributeValues(subAttributes[2].Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(subAttributes[2].ValueType));
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.RequiredAttributeMissing(
            attribute.Id,
            $"{article.ArticleNumber}_{article.CharacteristicId}",
            [subAttributes[1].Id]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypes))]
    public async Task UpdateAttributeValuesAsync_WhenAttributeHasRequiredSubAttributeWithMissingRequiredSubSubAttributes_ShouldReturnAttributeMissingError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttributes = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType);
        var subSubAttributes = AttributeFactory.AddSubAttributesTo(subAttributes[0], 2, attributeValueType);
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(subSubAttributes[0].Id, article.CharacteristicId, AttributeFactory.CreateAttributeValue(subSubAttributes[0].ValueType));
        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");

        var expectedError = AttributeErrors.RequiredAttributeMissing(
            subAttributes[0].Id,
            $"{article.ArticleNumber}_{article.CharacteristicId}",
            [subSubAttributes[1].Id]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypesWithoutBoolean))]
    public async Task UpdateAttributeValuesAsync_WhenValueTooHighOrTooLong_ShouldReturnValueTooLongOrTooHighError(AttributeValueType attributeValueType)
    {
        // Arrange
        const decimal maxLength = 2.000000m;
        const string valueAboveLimit = "99999";

        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];
        await dbContext.SaveChangesAsync();

        subAttribute.MaxLength = maxLength;
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, valueAboveLimit);

        var expectedError = AttributeErrors.ValueTooLongOrTooHigh(subAttribute.Id, valueAboveLimit, subAttribute.MaxLength.Value);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypesWithoutBoolean))]
    public async Task UpdateAttributeValuesAsync_WhenValueTooLowOrTooShort_ShouldReturnValueTooLowOrTooShortError(AttributeValueType attributeValueType)
    {
        // Arrange
        const decimal minLength = 10.000000m;
        const string valueBelowLimit = "1";

        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];

        subAttribute.MinLength = minLength;
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, valueBelowLimit);

        var expectedError = AttributeErrors.ValueTooShortOrTooLow(subAttribute.Id, valueBelowLimit, subAttribute.MinLength.Value);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypesWithoutBoolean))]
    public async Task UpdateAttributeValuesAsync_WhenValueNotInAllowedValues_ShouldReturnNotInAllowedValuesError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];

        subAttribute.AllowedValues = "1,2";
        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, "3");

        var expectedError = AttributeErrors.NotInAllowedValues(subAttribute.Id, ["3"], ["1", "2"]);

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Theory]
    [InlineData(AttributeValueType.Boolean)]
    [InlineData(AttributeValueType.Int)]
    [InlineData(AttributeValueType.Decimal)]
    public async Task UpdateAttributeValuesAsync_WhenWrongAttributeValueType_ShouldReturnWrongValueTypeError(AttributeValueType attributeValueType)
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var (_, article, attribute) = await AttributeTestData.CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, attributeValueType)[0];
        subAttribute.ValueType = attributeValueType;

        await dbContext.SaveChangesAsync();

        AddNewAttributeValues(attribute.Id, article.CharacteristicId, "True");
        AddNewAttributeValues(subAttribute.Id, article.CharacteristicId, "Invalid");

        var expectedError = AttributeErrors.WrongValueType(subAttribute.Id, "Invalid", attributeValueType.ToString());

        // Act
        var response = await UpdateAttributeValuesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [MemberData(nameof(InvalidRequestTestData))]
    public async Task UpdateAttributeValuesAsync_WhenRequestIsNotValid_ShouldReturnValidationError(InvalidRequestWithExpectedError testData)
    {
        // Arrange
        var request = _updateAttributeValuesRequest with { NewAttributeValues = testData.NewAttributeValues.ToArray() };

        // Act
        var response = await HttpClient.PutAsJsonAsync(EndpointRoutes.Attributes.UPDATE_ATTRIBUTE_VALUES, request);

        // Assert
        var errors = await ErrorResponseExtractor<UpdateAttributeValuesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(testData.ExpectedError);
    }

    private static (Attribute Home, Attribute MaterialValue) CreateAttributesForEnglishRootCategory(Article article, CqrsWriteDbContext dbContext)
    {
        var englishRootCategory = dbContext.RootCategories.Single(rootCategory => rootCategory.Id == TestConstants.RootCategory.ENGLISH_ROOT_CATEGORY_ID);

        var englishCategory = CategoryFactory.CreateCategory(1, "english-category", rootCategory: englishRootCategory);

        var home = AttributeFactory.CreateAttribute(
            name: "HOME",
            valueType: AttributeValueType.Boolean,
            minValues: 0,
            maxValues: 1,
            marketplaceAttributeIds: "HOME",
            rootCategory: englishRootCategory);

        var numberOfItems = AttributeFactory.CreateAttribute(
            name: "number_of_items",
            valueType: AttributeValueType.String,
            minValues: 0,
            maxValues: 1,
            marketplaceAttributeIds: "HOME,number_of_items",
            rootCategory: englishRootCategory);

        var numberOfItemsValueAttribute = AttributeFactory.CreateAttribute(
            name: "value",
            valueType: AttributeValueType.Int,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "HOME,number_of_items,value",
            rootCategory: englishRootCategory);

        var material = AttributeFactory.CreateAttribute(
            name: "material",
            valueType: AttributeValueType.String,
            minValues: 0,
            maxValues: 1,
            marketplaceAttributeIds: "HOME,material",
            rootCategory: englishRootCategory);

        var materialValue = AttributeFactory.CreateAttribute(
            name: "value",
            valueType: AttributeValueType.String,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "HOME,material,value",
            rootCategory: englishRootCategory);

        englishCategory.Attributes = [home];
        home.SubAttributes = [numberOfItems, material];
        numberOfItems.SubAttributes = [numberOfItemsValueAttribute];
        material.SubAttributes = [materialValue];

        home.AttributeBooleanValues = [new AttributeBooleanValue(true) { Article = article }];
        numberOfItemsValueAttribute.AttributeIntValues = [new AttributeIntValue(1) { Article = article }];

        article.Categories ??= [];
        article.Categories.Add(englishCategory);

        return (home, materialValue);
    }

    private static void CreateHomeProductTypeOnDeTreeWithNumberOfItemsSet(Article article, CqrsWriteDbContext dbContext)
    {
        var germanRootCategory = dbContext.RootCategories.Single(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);

        var germanCategory = CategoryFactory.CreateCategory(1, "german-category", rootCategory: germanRootCategory);

        var home = AttributeFactory.CreateAttribute(
            name: "HOME",
            valueType: AttributeValueType.Boolean,
            minValues: 0,
            maxValues: 1,
            marketplaceAttributeIds: "HOME",
            rootCategory: germanRootCategory);

        var numberOfItems = AttributeFactory.CreateAttribute(
            name: "number_of_items",
            valueType: AttributeValueType.String,
            minValues: 0,
            maxValues: 1,
            marketplaceAttributeIds: "HOME,number_of_items",
            rootCategory: germanRootCategory);

        var numberOfItemsValue = AttributeFactory.CreateAttribute(
            name: "value",
            valueType: AttributeValueType.Int,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "HOME,number_of_items,value",
            rootCategory: germanRootCategory);

        germanCategory.Attributes = [home];
        home.SubAttributes = [numberOfItems];
        numberOfItems.SubAttributes = [numberOfItemsValue];

        home.AttributeBooleanValues = [new AttributeBooleanValue(true) { Article = article }];
        numberOfItemsValue.AttributeIntValues = [new AttributeIntValue(1) { Article = article }];

        article.Categories ??= [];
        article.Categories!.Add(germanCategory);
    }

    private async Task ArticleShouldNotHaveValueForAttribute(int attributeId, Article article)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();

        var attribute = await dbContext.Attributes
            .Include(dbAttribute => dbAttribute.AttributeBooleanValues!)
            .ThenInclude(attributeValue => attributeValue.Article)
            .Include(dbAttribute => dbAttribute.AttributeIntValues!)
            .ThenInclude(attributeValue => attributeValue.Article)
            .Include(dbAttribute => dbAttribute.AttributeDecimalValues!)
            .ThenInclude(attributeValue => attributeValue.Article)
            .Include(dbAttribute => dbAttribute.AttributeStringValues!)
            .ThenInclude(attributeValue => attributeValue.Article)
            .SingleAsync(a => a.Id == attributeId);

        switch (attribute.ValueType)
        {
            case AttributeValueType.Boolean:
                attribute.AttributeBooleanValues!.Where(value => value.Article == article).Should().BeEmpty();
                break;
            case AttributeValueType.Int:
                attribute.AttributeIntValues!.Where(value => value.Article == article).Should().BeEmpty();
                break;
            case AttributeValueType.Decimal:
                attribute.AttributeDecimalValues!.Where(value => value.Article == article).Should().BeEmpty();
                break;
            case AttributeValueType.String:
                attribute.AttributeStringValues!.Where(value => value.Article == article).Should().BeEmpty();
                break;

            default:
                throw new ArgumentOutOfRangeException($"{attribute.ValueType}");
        }
    }

    private async Task ArticleShouldHaveCountAttributeValues(int articleId, int expectedBooleanCount, int expectedDecimalCount, int expectedIntCount, int expectedStringCount)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();

        var counts = await dbContext.Articles
            .Where(a => a.Id == articleId)
            .Include(a => a.AttributeBooleanValues)
            .Include(a => a.AttributeDecimalValues)
            .Include(a => a.AttributeIntValues)
            .Include(a => a.AttributeStringValues)
            .Select(article => new
            {
                BooleanCount = article.AttributeBooleanValues!.Count,
                DecimalCount = article.AttributeDecimalValues!.Count,
                IntCount = article.AttributeIntValues!.Count,
                StringCount = article.AttributeStringValues!.Count
            })
            .SingleAsync();

        counts.BooleanCount.Should().Be(expectedBooleanCount);
        counts.DecimalCount.Should().Be(expectedDecimalCount);
        counts.IntCount.Should().Be(expectedIntCount);
        counts.StringCount.Should().Be(expectedStringCount);
    }

    private async Task AttributeShouldHaveCountAttributeValues(int attributeId, int expectedBooleanCount, int expectedDecimalCount, int expectedIntCount, int expectedStringCount)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();

        var counts = await dbContext.Attributes
            .Where(a => a.Id == attributeId)
            .Include(a => a.AttributeBooleanValues)
            .Include(a => a.AttributeDecimalValues)
            .Include(a => a.AttributeIntValues)
            .Include(a => a.AttributeStringValues)
            .Select(article => new
            {
                BooleanCount = article.AttributeBooleanValues!.Count,
                DecimalCount = article.AttributeDecimalValues!.Count,
                IntCount = article.AttributeIntValues!.Count,
                StringCount = article.AttributeStringValues!.Count
            })
            .SingleAsync();

        counts.BooleanCount.Should().Be(expectedBooleanCount);
        counts.DecimalCount.Should().Be(expectedDecimalCount);
        counts.IntCount.Should().Be(expectedIntCount);
        counts.StringCount.Should().Be(expectedStringCount);
    }

    private async Task AttributesInDbShouldHaveValues(IReadOnlyCollection<Attribute> attributes, List<(string value, int Id)> values, int articleId)
    {
        foreach (var (value, attributeId) in values)
        {
            await AttributeInDbShouldHaveValue(attributes.Single(a => a.Id == attributeId), value, articleId);
        }
    }

    private async Task AttributeInDbShouldHaveValue(Attribute attribute, string attributeValue, int articleId)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();
        attribute = await dbContext.Attributes
            .Include(attr => attr.AttributeBooleanValues)
            .Include(attr => attr.AttributeDecimalValues)
            .Include(attr => attr.AttributeIntValues)
            .Include(attr => attr.AttributeStringValues)
            .SingleAsync(attr => attr.Id == attribute.Id);

        switch (attribute.ValueType)
        {
            case AttributeValueType.Boolean:
                attribute.AttributeBooleanValues!.Exists(value =>
                        value.ArticleId == articleId && value.Value == bool.Parse(attributeValue))
                    .Should().BeTrue();
                break;
            case AttributeValueType.Decimal:
                attribute.AttributeDecimalValues!.Exists(value =>
                        value.ArticleId == articleId && value.Value == decimal.Parse(attributeValue, CultureInfo.InvariantCulture))
                    .Should().BeTrue();
                break;
            case AttributeValueType.Int:
                attribute.AttributeIntValues!.Exists(value =>
                        value.ArticleId == articleId && value.Value == int.Parse(attributeValue, CultureInfo.InvariantCulture))
                    .Should().BeTrue();
                break;
            case AttributeValueType.String:
                attribute.AttributeStringValues!.Exists(value =>
                        value.ArticleId == articleId && string.Equals(value.Value, attributeValue, StringComparison.Ordinal))
                    .Should().BeTrue();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute.ValueType, "Unknown attribute value type");
        }
    }

    private List<(string, int Id)> CreateAndAddNewAttributeValuesForAttributes(AttributeValueType attributeValueType, List<Attribute> attributes, Article article)
        => CreateAndAddNewAttributeValuesForAttributes(attributeValueType, attributes, [article]).Single();

    private List<List<(string, int Id)>> CreateAndAddNewAttributeValuesForAttributes(AttributeValueType attributeValueType, List<Attribute> attributes, List<Article> articles)
    {
        var result = articles.ConvertAll(_ =>
            Enumerable
                .Range(0, attributes.Count)
                .Select(i => (AttributeFactory.CreateAttributeValue(attributeValueType), attributes[i].Id))
                .ToList());

        for (var i = 0; i < attributes.Count; i++)
        {
            AddNewAttributeValues(
                attributes[i].Id,
                articles.ConvertAll(article => article.CharacteristicId),
                result.ConvertAll(list => list[i].Item1));
        }

        return result;
    }

    private void AddNewAttributeValues(int attributeId, int characteristicId, string value)
        => AddNewAttributeValues(attributeId, [characteristicId], [value]);

    private void AddNewAttributeValues(int attributeId, IEnumerable<int> characteristicIds, IReadOnlyList<string> valueLists)
    {
        var innerAttributeValues = characteristicIds
            .Select((characteristicId, index) =>
                new VariantAttributeValues(
                    characteristicId,
                    index < valueLists.Count ? valueLists[index].Split(";").ToArray() : []))
            .ToList();

        _newAttributeValues.Add(new NewAttributeValue(attributeId, innerAttributeValues));
    }

    private async Task AssertStatusCodeForUpdateAttributeValuesAsync()
    {
        var response = await UpdateAttributeValuesAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<HttpResponseMessage> UpdateAttributeValuesAsync()
    {
        if (_newAttributeValues.Count == 0)
        {
            _newAttributeValues.Add(new NewAttributeValue(int.Parse(TestConstants.Attribute.ATTRIBUTE_ID, CultureInfo.InvariantCulture), [new VariantAttributeValues(TestConstants.Article.CHARACTERISTIC_ID, ["True"])]));
        }

        var request = _updateAttributeValuesRequest with { NewAttributeValues = _newAttributeValues.ToArray() };

        return await HttpClient.PutAsJsonAsync(
            EndpointRoutes.Attributes.UPDATE_ATTRIBUTE_VALUES,
            request);
    }
}
