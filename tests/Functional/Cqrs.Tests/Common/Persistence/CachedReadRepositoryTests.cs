using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.Exceptions;
using Cqrs.Api.Common.Interfaces;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Cqrs.Tests.TestCommon.BaseTest;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.Common.Persistence;

public class CachedReadRepositoryTests : BaseTestWithSharedCqrsApiFactory
{
    private readonly CqrsWriteDbContext _cqrsWriteDbContext;
    private readonly ICachedReadRepository<RootCategory> _cachedReadRepository;

    public CachedReadRepositoryTests(CqrsApiFactory factory)
        : base(factory)
    {
        _cqrsWriteDbContext = ResolveCqrsWriteDbContext();
        _cachedReadRepository = factory.Services.GetRequiredService<ICachedReadRepository<RootCategory>>();
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ShouldReturnAllItems()
    {
        // Arrange
        var expected = await _cqrsWriteDbContext.RootCategories.ToListAsync();

        // Act
        var actual = await _cachedReadRepository.GetAllAsync();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoItemsExist_ShouldThrowException()
    {
        // Arrange
        await _cqrsWriteDbContext.RootCategories.ExecuteDeleteAsync();

        // Act
        Func<Task> action = async () => await _cachedReadRepository.GetAllAsync();

        // Assert
        await action.Should().ThrowAsync<SeededTableIsEmptyException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_ShouldReturnItem()
    {
        // Arrange
        var expected = await _cqrsWriteDbContext.RootCategories.FirstAsync();

        // Act
        var actual = await _cachedReadRepository.GetByIdAsync(expected.Id);

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByIdAsync_WhenItemDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var expected = await _cqrsWriteDbContext.RootCategories.MaxAsync(e => e.Id) + 1;

        // Act
        var actual = await _cachedReadRepository.GetByIdAsync(expected);

        // Assert
        actual.Should().BeNull();
    }
}
