using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Categories.SearchCategories;
using Cqrs.Tests.TestCommon.BaseTest;
using Cqrs.Tests.TestCommon.ErrorHandling;
using Cqrs.Tests.TestCommon.Factories;
using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using TestCommon.ErrorHandling;

namespace Cqrs.Tests.UseCases.Categories.SearchCategories;

public class SearchCategoriesEndpointTests(TraditionalApiFactory factory)
    : BaseTestWithSharedTraditionalApiFactory(factory)
{
    private SearchCategoriesRequest _searchCategoriesRequest = new(
        TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
        TestConstants.Article.ARTILCE_NUMBER,
        CategoryNumber: null,
        SearchTerm: null);

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Happy Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [Description(
        """
        Scenario:
            The category 'Business Shelves' (5) is requested.
            - Once by the category number, and once by the search term.
            - It can be selected (mapped to an article) or not.
        Expectation:
            - The reponse should only contain one category tree.
            - It should include: 'Office' (1) -> 'Furniture' (3) -> 'Business Shelves' (5).
        """)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SearchCategoriesAsync_WhenSingleCategoryIsRequestedByIdOrSearchTerm_ShouldReturnOneCategoryTreeWithExpectedChildren(
        bool isSelected, bool isSearchTermRequest)
    {
        // Arrange
        if (isSearchTermRequest)
        {
            _searchCategoriesRequest = _searchCategoriesRequest with
            {
                SearchTerm = "Business Shelves"
            };
        }
        else
        {
            _searchCategoriesRequest = _searchCategoriesRequest with
            {
                CategoryNumber = 5
            };
        }

        await using var dbContext = ResolveTraditionalDbContext();

        var allCategories = (await GetAndAddTestCategories(dbContext)).ToArray();

        var office = allCategories.Single(category => category.CategoryNumber == 1);
        var furniture = allCategories.Single(category => category.CategoryNumber == 3);
        var businessShelves = allCategories.Single(category => category.CategoryNumber == 5);

        if (isSelected)
        {
            // Set the business shelves as the mapped category
            var article = ArticleFactory.CreateArticle(categories: [businessShelves]);
            await dbContext.Articles.AddAsync(article);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var categoryTrees = await GetCategoryTreeResponses();

        // Assert
        if (isSelected)
        {
            CategoryTreesContainsExpectedChildrenRecursively(categoryTrees, [office, furniture, businessShelves], selectedCategory: businessShelves);
        }
        else
        {
            CategoryTreesContainsExpectedChildrenRecursively(categoryTrees, [office, furniture, businessShelves]);
        }
    }

    [Fact]
    [Description(
        """
        Scenario:
            The search term 'Furniture' is requested.
            - No category is selected (mapped to an article).
        Expectation:
            - The reponse should contain two category trees: 'Office' (1) and 'Home' (2).
            - 'Office' (1) should inlcude: 'Furniture' (3).
            - 'Home' (2) should inlcude: 'Furniture' (4) -> 'Living Room Furniture' (6) + 'Kitchen Furniture and Accessories' (7) + 'Bathroom Furniture' (8).
        """)]
    public async Task SearchCategoriesAsync_WhenMultipleCategoryTreesAreRequestedBySearchTermFurniture_ShouldReturnMergedCategoryTree()
    {
        // Arrange
        _searchCategoriesRequest = _searchCategoriesRequest with
        {
            SearchTerm = "Furniture"
        };

        await using var dbContext = ResolveTraditionalDbContext();

        var allCategories = (await GetAndAddTestCategories(dbContext)).ToArray();

        var office = allCategories.Single(category => category.CategoryNumber == 1);
        var furniture = allCategories.Single(category => category.CategoryNumber == 3);

        var home = allCategories.Single(category => category.CategoryNumber == 2);
        var homeFurniture = allCategories.Single(category => category.CategoryNumber == 4);
        var livingRoomFurniture = allCategories.Single(category => category.CategoryNumber == 6);
        var kitchenFurnitureAndAccessories = allCategories.Single(category => category.CategoryNumber == 7);
        var bathroomFurniture = allCategories.Single(category => category.CategoryNumber == 8);

        List<Category> expectedChildren = [office, furniture, home, homeFurniture, livingRoomFurniture, kitchenFurnitureAndAccessories, bathroomFurniture];

        // Act
        var categoryTrees = await GetCategoryTreeResponses();

        // Assert
        CategoryTreesContainsExpectedChildrenRecursively(categoryTrees, expectedChildren);
    }

    [Fact]
    [Description(
        """
        Scenario:
            The search term 'Shelves' is requested.
            - No category is selected (mapped to an article).
        Expectation:
            - The reponse should contain two category trees: 'Office' (1) and 'Home' (2).
            - 'Office' (1) should inlcude: 'Furniture' (3) -> 'Business Shelves' (5).
            - 'Home' (2) should inlcude:
            'Furniture' (4) ->
                - 'Kitchen Furniture and Accessories' (7) -> 'Storage Shelves' (10)
                - 'Living Room Furniture' (6) -> 'TV Shelves' (9)
        """)]
    public async Task SearchCategoriesAsync_WhenMultipleCategoryTreesAreRequestedBySearchTermShelves_ShouldReturnMergedCategoryTree()
    {
        // Arrange
        _searchCategoriesRequest = _searchCategoriesRequest with
        {
            SearchTerm = "Shelves"
        };

        await using var dbContext = ResolveTraditionalDbContext();

        var allCategories = (await GetAndAddTestCategories(dbContext)).ToArray();

        var office = allCategories.Single(category => category.CategoryNumber == 1);
        var furniture = allCategories.Single(category => category.CategoryNumber == 3);
        var businessShelves = allCategories.Single(category => category.CategoryNumber == 5);

        var home = allCategories.Single(category => category.CategoryNumber == 2);
        var homeFurniture = allCategories.Single(category => category.CategoryNumber == 4);
        var livingRoomFurniture = allCategories.Single(category => category.CategoryNumber == 6);
        var tvShelves = allCategories.Single(category => category.CategoryNumber == 9);
        var kitchenFurnitureAndAccessories = allCategories.Single(category => category.CategoryNumber == 7);
        var storageShelves = allCategories.Single(category => category.CategoryNumber == 10);

        List<Category> expectedChildren = [office, furniture, businessShelves, home, homeFurniture, livingRoomFurniture, tvShelves, kitchenFurnitureAndAccessories, storageShelves];

        // Act
        var categoryTrees = await GetCategoryTreeResponses();

        // Assert
        CategoryTreesContainsExpectedChildrenRecursively(categoryTrees, expectedChildren);
    }

    /*--------------------------------------------------------------------------------------------------
    ----------------------------------------- Error Path Tests -----------------------------------------
    --------------------------------------------------------------------------------------------------*/

    [Fact]
    public async Task SearchCategoriesAsync_WhenCategoryNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _searchCategoriesRequest = _searchCategoriesRequest with
        {
            CategoryNumber = 999
        };

        var expectedError = CategoryErrors.NoResultsForCategorySearch(_searchCategoriesRequest);

        // Act
        var response = await SearchCategoriesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<SearchCategoriesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.NotFound);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    /*--------------------------------------------------------------------------------------------------
    -------------------------- Request Validation Filter with Validator Tests --------------------------
    --------------------------------------------------------------------------------------------------*/

    [Theory]
    [InlineData(null, null)]
    [InlineData(0L, null)]
    [InlineData(1L, "abc")]
    public async Task SearchCategoriesAsync_WhenRequestIsNotValid_ShouldReturnBadRequestWithValidationError(
        long? categoryNumber, string? searchTerm)
    {
        // Arrange
        _searchCategoriesRequest = _searchCategoriesRequest with
        {
            CategoryNumber = categoryNumber,
            SearchTerm = searchTerm
        };

        var expectedError = categoryNumber is null or 0 && string.IsNullOrWhiteSpace(searchTerm)
            ? Error.Validation(
                code: "SearchTermOrCategoryNumber",
                description: "Either the search term or the category number must be set.")
            : Error.Validation(
                code: "SearchTermAndCategoryNumber",
                description: "You can't set both the search term and category number.");

        // Act
        var response = await SearchCategoriesAsync();

        // Assert
        var errors = await ErrorResponseExtractor<SearchCategoriesRequest>
            .ValidateResponseAndGetErrorsAsync(response, HttpStatusCode.BadRequest);

        errors.ShouldContainSingleEquivalentTo(expectedError);
    }

    [Description(
        """
        The category tree is as follows:

        - Office (1)
           - Furniture (3)
               - Business Shelves (5)

        - Home (2)
           - Furniture (4)
               - Living Room Furniture (6)
                   - TV Shelves (9)
               - Kitchen Furniture and Accessories (7)
                   - Storage Shelves (10)
               - Bathroom Furniture (8)

        Classic search criteria:
        - by some id
        - by a search tream which results in multiple category trees
            - "Furniture" results in two category trees (But the actual result will have to be merged for the response)
            - "Shelves" results in two category trees (But the actual result will have to be merged for the response)
        """)]
    private static async Task<IEnumerable<Category>> GetAndAddTestCategories(TraditionalDbContext dbContext)
    {
        // All categories are under the German root category
        var germanRootCategory = await dbContext.RootCategories
            .SingleAsync(x => x.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);

        // Office (1)
        var office = CategoryFactory.CreateCategory(categoryNumber: 1, name: "Office", rootCategory: germanRootCategory);
        var officeFurniture = CategoryFactory.CreateCategory(categoryNumber: 3, name: "Furniture", isLeaf: true, rootCategory: germanRootCategory, parent: office);
        var businessShelves = CategoryFactory.CreateCategory(categoryNumber: 5, name: "Business Shelves", isLeaf: true, rootCategory: germanRootCategory, parent: officeFurniture);

        // Home (2)
        var home = CategoryFactory.CreateCategory(categoryNumber: 2, name: "Home", rootCategory: germanRootCategory);
        var homeFurniture = CategoryFactory.CreateCategory(categoryNumber: 4, name: "Furniture", isLeaf: true, rootCategory: germanRootCategory, parent: home);

        var livingRoomFurniture = CategoryFactory.CreateCategory(categoryNumber: 6, name: "Living Room Furniture", isLeaf: true, rootCategory: germanRootCategory, parent: homeFurniture);
        var tvShelves = CategoryFactory.CreateCategory(categoryNumber: 9, name: "TV Shelves", isLeaf: true, rootCategory: germanRootCategory, parent: livingRoomFurniture);

        var kitchenFurnitureAndAccessories = CategoryFactory.CreateCategory(categoryNumber: 7, name: "Kitchen Furniture and Accessories", isLeaf: true, rootCategory: germanRootCategory, parent: homeFurniture);
        var storageShelves = CategoryFactory.CreateCategory(categoryNumber: 10, name: "Storage Shelves", isLeaf: true, rootCategory: germanRootCategory, parent: kitchenFurnitureAndAccessories);

        var bathroomFurniture = CategoryFactory.CreateCategory(categoryNumber: 8, name: "Bathroom Furniture", isLeaf: true, rootCategory: germanRootCategory, parent: homeFurniture);

        List<Category> categories = [office, officeFurniture, businessShelves, home, homeFurniture, livingRoomFurniture, tvShelves, kitchenFurnitureAndAccessories, storageShelves, bathroomFurniture];

        await dbContext.Categories.AddRangeAsync(categories);
        await dbContext.SaveChangesAsync();

        return categories;
    }

    private static void CategoryTreesContainsExpectedChildrenRecursively(
        IEnumerable<SearchCategoriesResponse> categoryTrees,
        IReadOnlyCollection<Category> expectedChildren,
        Category? selectedCategory = null)
    {
        expectedChildren.Should().NotBeEmpty();

        foreach (var categoryTree in categoryTrees)
        {
            // Assert the category tree (top level parent category)
            var expectedCategory = expectedChildren.Single(expectedChild =>
                expectedChild.CategoryNumber == categoryTree.CategoryNumber);

            categoryTree.Label.Should().Be(expectedCategory.Name);
            categoryTree.IsLeaf.Should().Be(expectedCategory.IsLeaf).And.BeFalse();

            CheckChildren(expectedChildren, selectedCategory, categoryTree);
        }
    }

    private static void CheckChildren(
        IReadOnlyCollection<Category> expectedChildren,
        Category? selectedCategory,
        SearchCategoriesResponse searchCategoriesResponse)
    {
        var children = searchCategoriesResponse.Children.ToArray();
        var childrenCategoryNumbers = children.Select(child => child.CategoryNumber);

        // The current children should be in the 'expectedChildren'
        var currentExpectedChildren = expectedChildren.Where(expectedChild =>
                childrenCategoryNumbers.Contains(expectedChild.CategoryNumber))
            .ToArray();

        // The current children should be the same as the expected children
        children.Length.Should().Be(currentExpectedChildren.Length);

        foreach (var expectedChild in currentExpectedChildren)
        {
            var child = children.Single(child =>
                child.CategoryNumber == expectedChild.CategoryNumber);

            child.Label.Should().Be(expectedChild.Name);
            child.IsLeaf.Should().BeTrue();

            if (expectedChild.CategoryNumber == selectedCategory?.CategoryNumber)
            {
                child.IsSelected.Should().BeTrue();
            }
            else
            {
                child.IsSelected.Should().BeFalse();
            }

            var nextExpectedChildren = expectedChildren.Except(currentExpectedChildren).ToArray();

            // Recursively check the children of the current child
            CheckChildren(nextExpectedChildren, selectedCategory, child);
        }
    }

    private async Task<SearchCategoriesResponse[]> GetCategoryTreeResponses()
    {
        var response = await SearchCategoriesAsync();
        response.EnsureSuccessStatusCode();

        var categoryTreeResponses = await response.Content.ReadFromJsonAsync<SearchCategoriesResponse[]>();
        categoryTreeResponses.Should().NotBeNull();

        return categoryTreeResponses!;
    }

    private async Task<HttpResponseMessage> SearchCategoriesAsync()
    {
        return await HttpClient.GetAsync(CreateRequestUri());

        string CreateRequestUri()
        {
            var uriBuilder = new UriBuilder(HttpClient.BaseAddress!)
            {
                Path = EndpointRoutes.Categories.SEARCH_CATEGORIES
            };

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["RootCategoryId"] = _searchCategoriesRequest.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            query["ArticleNumber"] = _searchCategoriesRequest.ArticleNumber;

            // Error case: both search term and category number are set
            if (_searchCategoriesRequest.CategoryNumber is not null && _searchCategoriesRequest.SearchTerm is not null)
            {
                query["CategoryNumber"] = _searchCategoriesRequest.CategoryNumber?.ToString(CultureInfo.InvariantCulture);
                query["SearchTerm"] = _searchCategoriesRequest.SearchTerm;
            }
            else if (_searchCategoriesRequest.CategoryNumber is not null)
            {
                query.Remove("SearchTerm");
                query["CategoryNumber"] = _searchCategoriesRequest.CategoryNumber?.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                query.Remove("CategoryNumber");
                query["SearchTerm"] = _searchCategoriesRequest.SearchTerm;
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.PathAndQuery;
        }
    }
}
