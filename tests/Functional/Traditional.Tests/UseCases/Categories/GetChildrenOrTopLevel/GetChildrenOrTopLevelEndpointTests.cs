using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.Factories;
using Traditional.Api.UseCases.Articles.Persistence.Entities;
using Traditional.Api.UseCases.Categories.Common.Errors;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;
using Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;
using Traditional.Tests.TestCommon.BaseTest;
using Traditional.Tests.TestCommon.ErrorHandling;

namespace Traditional.Tests.UseCases.Categories.GetChildrenOrTopLevel;

public class GetChildrenOrTopLevelEndpointTests(TraditionalApiFactory factory)
    : BaseTestWithSharedTraditionalApiFactory(factory)
{
    private GetChildrenOrTopLevelRequest _getChildrenOrTopLevelRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER,
        CategoryNumber: null);

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    [Description(
        """
        Scenario:
            The top level categories are requested.
            - The CategoryNumber of the request is null.
            - Some top level categories exist.
        Expectation:
            - The top level categories should be returned.
        """)]
    public async Task GetGetChildrenOrTopLevelAsync_WhenTopLevelCategoriesExist_ShouldReturnTopLevelCategories()
    {
        // Arrange
        var topLevelCategories = CategoryFactory.CreateCategories(3).ToList();
        await AddCategoriesWithGermanRoot(topLevelCategories);

        // Act
        var categories = await GetChildrenOrTopLevelResponses();

        // Assert
        CategoriesShouldOnlyContain(categories, topLevelCategories);
    }

    [Fact]
    [Description(
        """
        Scenario:
            The children of a random category are requested.
            - The CategoryNumber of the request is not null.
            - The partent category exists.
            - The child categories do not exist.
        Expectation:
            - An empty list should be returned since the category does not have children.
        """)]
    public async Task GetGetChildrenOrTopLevelAsync_WhenParentHasNoChildren_ShouldReturnEmptyList()
    {
        // Arrange
        var category = CategoryFactory.CreateCategory();
        await AddCategoriesWithGermanRoot([category]);

        _getChildrenOrTopLevelRequest = _getChildrenOrTopLevelRequest with
        {
            CategoryNumber = category.CategoryNumber
        };

        // Act
        var categories = await GetChildrenOrTopLevelResponses();

        // Assert
        categories.Should().BeEmpty();
    }

    [Fact]
    [Description(
        """
        Scenario:
            The children of a random category are requested.
            - The CategoryNumber of the request is not null.
            - The partent category exists and has children.
        Expectation:
            - The child categories should be returned.
        """)]
    public async Task GetGetChildrenOrTopLevelAsync_WhenParentHasChildren_ShouldReturnChildCategories()
    {
        // Arrange
        var parentCategory = CategoryFactory.CreateCategory();
        var children = CategoryFactory.CreateCategories(2, parent: parentCategory, isLeaf: true).ToList();
        await AddCategoriesWithGermanRoot([.. children, parentCategory]);

        _getChildrenOrTopLevelRequest = _getChildrenOrTopLevelRequest with
        {
            CategoryNumber = parentCategory.CategoryNumber
        };

        // Act
        var categories = await GetChildrenOrTopLevelResponses();

        // Assert
        CategoriesShouldOnlyContain(categories, children);
    }

    [Fact]
    [Description(
        """
        Scenario:
            The top level categories are requested.
            - The CategoryNumber of the request is null.
            - Some top level categories exist and one of them is mapped to the article.
        Expectation:
            - The top level categories should be returned with the IsSelected property set.
        """)]
    public async Task GetGetChildrenOrTopLevelAsync_WhenOneCategoryIsMapped_ShouldReturnOneCategoryWithIsSelectedTrue()
    {
        // Arrange
        var topLevelCategories = CategoryFactory.CreateCategories(3).ToList();
        var article = ArticleFactory.CreateArticle(categories: [topLevelCategories[0]]);

        await AddCategoriesWithGermanRoot(topLevelCategories, article);

        // Act
        var categories = await GetChildrenOrTopLevelResponses();

        // Assert
        CategoriesShouldOnlyContain(categories, topLevelCategories, selectedCategory: topLevelCategories[0]);
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Description(
        """
        Scenario:
            All top level categories or the children of a random category are requested.
            - The CategoryNumber of the request is null (top level is requested) or not null (specific children are requested).
            - The partent category does not exist.
            - The child categories do not exist.
        Expectation:
            - CategoryNumber is null:
                - An error should be returned that the categories were not found.
            - CategoryNumber is not null:
                - An empty list should be returned since the category does not have children.
        """)]
    public async Task GetGetChildrenOrTopLevelAsync_WhenNeitherParentNorChildCategoriesExist_ShouldReturnErrorOrEmptyList(bool isCategoryNumberNull)
    {
        // Arrange
        _getChildrenOrTopLevelRequest = _getChildrenOrTopLevelRequest with
        {
            CategoryNumber = isCategoryNumberNull
                ? null
                : TestConstants.Category.CATEGORY_NUMBER
        };

        var expectedError = isCategoryNumberNull
            ? CategoryErrors.CategoriesNotFound(_getChildrenOrTopLevelRequest.RootCategoryId)
            : CategoryErrors.CategoryNotFound(_getChildrenOrTopLevelRequest.CategoryNumber!.Value, _getChildrenOrTopLevelRequest.RootCategoryId);

        // Act
        var response = await GetChildrenOrTopLevelAsync();

        // Assert
        var errors = await ErrorResponseExtractor<GetChildrenOrTopLevelRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------
    Note: Since the CategoryNumber is nullable the validations in not tested here but in the GetCategoryMappingEndpointTests*/

    private static void CategoriesShouldOnlyContain(
        IEnumerable<GetChildrenOrTopLevelResponse> categories,
        IEnumerable<Category> expectedCategories,
        Category? selectedCategory = null)
    {
        var orderedCategories = categories.OrderBy(x => x.CategoryNumber).ToArray();
        var orderedExpectedCategories = expectedCategories.OrderBy(x => x.CategoryNumber).ToArray();

        orderedCategories.Length.Should().Be(orderedExpectedCategories.Length);

        for (int i = 0; i < orderedCategories.Length; i++)
        {
            var category = orderedCategories[i];
            var expectedCategory = orderedExpectedCategories[i];
            bool isLeaf = expectedCategory.Parent is not null;

            category.CategoryNumber.Should().Be(expectedCategory.CategoryNumber);
            category.Label.Should().Be(expectedCategory.Name);
            category.IsLeaf.Should().Be(expectedCategory.IsLeaf).And.Be(isLeaf);
            if (selectedCategory is not null)
            {
                category.IsSelected.Should().Be(category.CategoryNumber == selectedCategory.CategoryNumber);
            }
        }
    }

    private async Task AddCategoriesWithGermanRoot(List<Category> categories, Article? article = null)
    {
        await using var dbContext = ResolveTraditionalDbContext();

        var germanRootCategory = await dbContext.RootCategories
            .SingleAsync(x => x.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);

        foreach (var category in categories)
        {
            category.RootCategory = germanRootCategory;
        }

        if (article is not null)
        {
            await dbContext.Articles.AddAsync(article);
        }

        await dbContext.Categories.AddRangeAsync(categories);
        await dbContext.SaveChangesAsync();
    }

    private async Task<GetChildrenOrTopLevelResponse[]> GetChildrenOrTopLevelResponses()
    {
        var response = await GetChildrenOrTopLevelAsync();
        response.EnsureSuccessStatusCode();

        var childrenOrTopLevelResponses = await response.Content.ReadFromJsonAsync<GetChildrenOrTopLevelResponse[]>();
        childrenOrTopLevelResponses.Should().NotBeNull();

        return childrenOrTopLevelResponses!;
    }

    private async Task<HttpResponseMessage> GetChildrenOrTopLevelAsync()
    {
        return await HttpClient.GetAsync(CreateRequestUri());

        string CreateRequestUri()
        {
            var uriBuilder = new UriBuilder(HttpClient.BaseAddress!)
            {
                Path = EndpointRoutes.Categories.GET_CHILDREN_OR_TOP_LEVEL
            };

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["RootCategoryId"] = _getChildrenOrTopLevelRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            query["ArticleNumber"] = _getChildrenOrTopLevelRequest.ArticleNumber;

            if (_getChildrenOrTopLevelRequest.CategoryNumber is not null)
            {
                query["CategoryNumber"] = _getChildrenOrTopLevelRequest
                    .CategoryNumber?.ToString(CultureInfo.InvariantCulture);
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.PathAndQuery;
        }
    }
}
