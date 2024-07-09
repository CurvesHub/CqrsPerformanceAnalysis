using System.Net;
using System.Net.Http.Json;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Categories.Commands.UpdateCategoryMapping;
using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Tests.TestCommon.BaseTest;
using Cqrs.Tests.TestCommon.ErrorHandling;
using Cqrs.Tests.TestCommon.Factories;
using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.ErrorHandling;

namespace Cqrs.Tests.UseCases.Categories.UpdateCategoryMapping;

public class UpdateCategoryMappingEndpointTests(CqrsApiFactory factory)
    : BaseTestWithSharedCqrsApiFactory(factory)
{
    private UpdateCategoryMappingCommand _updateCategoryMappingCommand = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER,
        TestConstants.Category.CATEGORY_NUMBER);

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task UpdateCategoryMappingAsync_WhenArticleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var germanRootCategory = await dbContext.RootCategories
                .SingleAsync(x => x.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);

        var office = CategoryFactory.CreateCategory(
            categoryNumber: TestConstants.Category.CATEGORY_NUMBER,
            name: "Office",
            rootCategory: germanRootCategory);

        await dbContext.Categories.AddAsync(office);
        await dbContext.SaveChangesAsync();

        var expectedError = ArticleErrors.ArticleNotFound(_updateCategoryMappingCommand.ArticleNumber);

        // Act
        var response = await UpdateCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateCategoryMappingCommand>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Fact]
    public async Task UpdateCategoryMappingAsync_WhenArticleHasNoCategories_ShouldUpdateCategoryMapping()
    {
        // Arrange
        var (articleIds, expectedNewCategory) = await SetupArticlesWithCategories(
            _updateCategoryMappingCommand.CategoryNumber, addOldCategory: false);

        // Act and Assert
        await ResponseShouldOnlyContain(expectedNewCategory);
        await ArticlesShouldAllOnlyBeMappedTo(articleIds, expectedNewCategory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task UpdateCategoryMappingAsync_WhenArticleHasOneCategoryAndMultipleVariants_ShouldUpdateCategoryMappingForAllFoundArticles(
        int variantCount)
    {
        // Arrange
        const long newCategoryNumber = TestConstants.Category.CATEGORY_NUMBER + 1;
        _updateCategoryMappingCommand = _updateCategoryMappingCommand with
        {
            CategoryNumber = newCategoryNumber
        };

        var (articleIds, expectedNewCategory) = await SetupArticlesWithCategories(
            newCategoryNumber,
            articleCount: variantCount + 1);

        // Act and Assert
        await ResponseShouldOnlyContain(expectedNewCategory);
        await ArticlesShouldAllOnlyBeMappedTo(articleIds, expectedNewCategory);
    }

    [Fact]
    public async Task UpdateCategoryMappingAsync_WhenArticleHasCategoriesOfDifferentRootCategory_ShouldUpdateCategoryMappingOnlyForTheRequestedRootCategory()
    {
        // Arrange
        const long newCategoryNumber = TestConstants.Category.CATEGORY_NUMBER + 1;
        _updateCategoryMappingCommand = _updateCategoryMappingCommand with
        {
            CategoryNumber = newCategoryNumber
        };

        var (articleIds, expectedNewCategory) = await SetupArticlesWithCategories(
            newCategoryNumber,
            hasCategoriesOfDifferentRootCategory: true);

        // Act and Assert
        await ResponseShouldOnlyContain(expectedNewCategory);
        await ArticlesShouldAllOnlyBeMappedTo(articleIds, expectedNewCategory, hasCategoriesOfDifferentRootCategory: true);
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task UpdateCategoryMappingAsync_WhenCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        await using var dbContext = ResolveCqrsWriteDbContext();
        var article = ArticleFactory.CreateArticle();

        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        var expectedError = CategoryErrors.CategoryNotFound(_updateCategoryMappingCommand.CategoryNumber, _updateCategoryMappingCommand.RootCategoryId);

        // Act
        var response = await UpdateCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateCategoryMappingCommand>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCategoryMappingAsync_WhenRequestIsNotValid_ShouldReturnBadRequestWithValidationError(long categoryNumber)
    {
        // Arrange
        _updateCategoryMappingCommand = _updateCategoryMappingCommand with
        {
            CategoryNumber = categoryNumber
        };

        var expectedError = Error.Validation(
            code: "CategoryNumber",
            description: "The value of 'Category Number' must be greater than '0'.");

        // Act
        var response = await UpdateCategoryMappingAsync();

        // Assert
        var errors = await ErrorResponseExtractor<UpdateCategoryMappingCommand>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    private async Task<(IEnumerable<int> articleIds, Category expectedNewCategory)> SetupArticlesWithCategories(
        long newCategoryNumber,
        bool addOldCategory = true,
        bool hasCategoriesOfDifferentRootCategory = false,
        int articleCount = 1)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();

        var germanRootCategory = await dbContext.RootCategories
            .SingleAsync(x => x.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);

        var articles = ArticleFactory.CreateVariants(articleCount).ToList();
        articles.ForEach(article => article.Categories ??= []);

        if (hasCategoriesOfDifferentRootCategory)
        {
            var frenchRootCategory = await dbContext.RootCategories
                .SingleAsync(x => x.Id == TestConstants.RootCategory.FRENCH_ROOT_CATEGORY_ID);

            var frenchCategory = CategoryFactory.CreateCategory(
                categoryNumber: TestConstants.Category.CATEGORY_NUMBER,
                name: "French Category",
                path: "French Category",
                rootCategory: frenchRootCategory);

            articles.ForEach(article => article.Categories!.Add(frenchCategory));

            await dbContext.Categories.AddAsync(frenchCategory);
        }

        if (addOldCategory)
        {
            var oldCategory = CategoryFactory.CreateCategory(
                categoryNumber: TestConstants.Category.CATEGORY_NUMBER,
                name: "Old Category",
                path: "Old Category",
                rootCategory: germanRootCategory);

            articles.ForEach(article => article.Categories!.Add(oldCategory));

            await dbContext.Categories.AddAsync(oldCategory);
        }

        var newCategory = CategoryFactory.CreateCategory(
            categoryNumber: newCategoryNumber,
            name: "New Category",
            path: "New Category",
            rootCategory: germanRootCategory);

        await dbContext.Categories.AddAsync(newCategory);
        await dbContext.Articles.AddRangeAsync(articles);
        await dbContext.SaveChangesAsync();

        return (articles.Select(article => article.Id), newCategory);
    }

    private async Task ArticlesShouldAllOnlyBeMappedTo(
        IEnumerable<int> articleIds,
        Category expectedNewCategory,
        bool hasCategoriesOfDifferentRootCategory = false)
    {
        await using var dbContext = ResolveCqrsWriteDbContext();

        var articles = await dbContext.Articles
            .Include(article => article.Categories)
            .Where(article => articleIds.Contains(article.Id))
            .ToListAsync();

        foreach (var article in articles)
        {
            Category newCategory;
            if (hasCategoriesOfDifferentRootCategory)
            {
                newCategory = article.Categories.Should().ContainSingle(category => category.Id == expectedNewCategory.Id).Which;

                article.Categories!
                    .Where(category => category.Id != expectedNewCategory.Id)
                    .Should().OnlyContain(category => category.RootCategoryId != expectedNewCategory.RootCategoryId);
            }
            else
            {
                newCategory = article.Categories.Should().ContainSingle().Which;
            }

            /*
            We need to exclude the 'c.Articles' and the 'c.RootCategory' from the comparison
            because those are navigation properties which are loaded differently for the 'newCategory'.

            *   'c.Articles' for the 'newCategory' include now the updated article
                but for the 'expectedNewCategory' it does not since it's created before the update.

            *   'c.RootCategory' for the 'newCategory' is the same as the 'expectedNewCategory'
                but it's not loaded for the 'newCategory' since it's not included in the query.

            *   The rest of the properties should be the same. (e.g. 'Id', 'RootCategoryId', 'CategoryNumber')
            */
            newCategory.Should().BeEquivalentTo(
                expectedNewCategory,
                options => options
                    .Excluding(c => c.Articles)
                    .Excluding(c => c.RootCategory));
        }
    }

    private async Task ResponseShouldOnlyContain(Category expectedNewCategory)
    {
        var response = await UpdateCategoryMappingAsync();
        response.EnsureSuccessStatusCode();

        var newCategory = await response.Content.ReadFromJsonAsync<UpdatedCategoryMappingResponse>();
        newCategory.Should().NotBeNull();

        newCategory!.CategoryNumber.Should().Be(expectedNewCategory.CategoryNumber);
        newCategory.CategoryPath.Should().Be(expectedNewCategory.Path);
    }

    private async Task<HttpResponseMessage> UpdateCategoryMappingAsync()
    {
        return await HttpClient.PutAsJsonAsync(
            EndpointRoutes.Categories.UPDATE_CATEGORY_MAPPING,
            _updateCategoryMappingCommand);
    }
}
