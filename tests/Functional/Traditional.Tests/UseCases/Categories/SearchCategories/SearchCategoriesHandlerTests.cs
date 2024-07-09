using System.ComponentModel;
using FluentAssertions;
using NSubstitute;
using TestCommon.Constants;
using Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;
using Traditional.Api.UseCases.Categories.SearchCategories;

namespace Traditional.Tests.UseCases.Categories.SearchCategories;

public class SearchCategoriesHandlerTests
{
    [Fact]
    [Description(
        """
        Scenario:
            The request validation fails and an invalid request hits the handler.
            - The category number is null.
            - The search term is null.
        Expectation:
            - An expection should be thrown before categories are loaded.
        """)]
    public async Task SearchCategoriesAsync_WhenValidatorFailsAndRequestIsInvalid_ShouldThrowException()
    {
        // Arrange
        var request = new SearchCategoriesRequest(
            TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
            TestConstants.Article.ARTILCE_NUMBER,
            CategoryNumber: null,
            SearchTerm: null);

        var handler = new SearchCategoriesHandler(Substitute.For<ICategoryRepository>());

        // Act
        Func<Task> act = async () => await handler.SearchCategoriesAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Either the search term or the category number must be provided.");
    }
}
