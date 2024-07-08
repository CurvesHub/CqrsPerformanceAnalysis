using System.ComponentModel;
using Cqrs.Api.Common.DataAccess.Entities;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace Cqrs.Tests.Common.Persistence;

public class CacheTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Description(
        """
        Scenario:
            The cache is tested with BaseEntity as TItem.
            The cache can be empty or not.
        Expected:
            The cache should return base entities and set them if it is empty.
        """)]
    public async Task GetOrSetAllAsync_WhenCalled_ShouldReturnBaseEntitiesAndSetCacheIfEmpty(
        bool isCacheEmpty)
    {
        // Arrange
        const string key = nameof(BaseEntity);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new Cache<BaseEntity>(memoryCache);

        List<BaseEntity> baseEntities = [new BaseEntity { Id = 1 }, new BaseEntity { Id = 2 }];

        if (!isCacheEmpty)
        {
            memoryCache.Set(key, baseEntities);
        }

        // Act
        var retrievedBaseEntities = await cache
            .GetOrSetAllAsync(async () => await Task.FromResult(baseEntities));

        // Assert
        retrievedBaseEntities.Should().BeEquivalentTo(baseEntities);
        memoryCache.Get(key).Should().BeEquivalentTo(baseEntities);
    }

    [Fact]
    [Description(
        """
        Scenario:
            The cache is tested with BaseEntity and RootCategory as TItem.
            The cache is empty.
        Expected:
            The cache should return base entities and set them.
            The cache should return root categories and set them.
            The cache should have different expiration times for RootCategory and BaseEntity.
            RootCategory should have an expiration time of 1 day.
            BaseEntity should have an expiration time of 1 hour.
        """)]
    public async Task GetOrSetAllAsync_WhenCalled_ShouldReturnBaseEntitiesAndRootCategoriesAndSetCacheWithCorrectExpirationTime()
    {
        // Arrange
        const string baseEntityKey = nameof(BaseEntity);
        const string rootCategoryKey = nameof(RootCategory);

        var testClock = new TestableSystemClock(DateTimeOffset.UtcNow);
        var memoryCache = new MemoryCache(new MemoryCacheOptions { Clock = testClock });

        var baseEntityCache = new Cache<BaseEntity>(memoryCache);
        var rootCategoryCache = new Cache<RootCategory>(memoryCache);

        List<BaseEntity> baseEntities = [new BaseEntity { Id = 1 }, new BaseEntity { Id = 2 }];
        List<RootCategory> rootCategories = [new RootCategory(LocaleCode.de_DE), new RootCategory(LocaleCode.fr_FR)];

        // Act
        var retrievedBaseEntities = await baseEntityCache
            .GetOrSetAllAsync(async () => await Task.FromResult(baseEntities));

        var retrievedRootCategories = await rootCategoryCache
            .GetOrSetAllAsync(async () => await Task.FromResult(rootCategories));

        // Assert
        memoryCache.Count.Should().Be(2);

        retrievedBaseEntities.Should().BeEquivalentTo(baseEntities);
        memoryCache.Get(baseEntityKey).Should().BeEquivalentTo(baseEntities);

        retrievedRootCategories.Should().BeEquivalentTo(rootCategories);
        memoryCache.Get(rootCategoryKey).Should().BeEquivalentTo(rootCategories);

        memoryCache.Get(baseEntityKey).Should().NotBeSameAs(memoryCache.Get(rootCategoryKey));

        // Advance the clock by 1 hour
        testClock.UtcNow = testClock.UtcNow.AddHours(1);
        memoryCache.Get(baseEntityKey).Should().BeNull();
        memoryCache.Get(rootCategoryKey).Should().BeEquivalentTo(rootCategories);

        // Advance the clock by 1 day
        testClock.UtcNow = testClock.UtcNow.AddHours(23);
        memoryCache.Get(baseEntityKey).Should().BeNull();
        memoryCache.Get(rootCategoryKey).Should().BeNull();
    }

    private class TestableSystemClock(DateTimeOffset initialTime) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = initialTime;
    }
}
