using System.ComponentModel;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;
using Cqrs.Api.UseCases.Categories.Queries.SearchCategories;
using FluentAssertions;
using NSubstitute;
using TestCommon.Constants;

namespace Cqrs.Tests.UseCases.Categories.SearchCategories;

public class SearchCategoriesQueryHandlerTests
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
        var request = new SearchCategoriesQuery(
            TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID,
            TestConstants.Article.ARTILCE_NUMBER,
            CategoryNumber: null,
            SearchTerm: null);

        var handler = new SearchCategoriesQueryHandler(Substitute.For<ICategoryReadRepository>());

        // Act
        Func<Task> act = async () => await handler.SearchCategoriesAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Either the search term or the category number must be provided.");
    }
}
