using System.Diagnostics;
using System.Globalization;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Articles.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Traditional.Tests.TestCommon.Factories;
using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;
using ILogger = Serilog.ILogger;

namespace PerformanceTests.Common.Services;

/// <summary>
/// Provides the data setup for the traditional database.
/// </summary>
/// <param name="_traditionalDbContext">The traditional database context.</param>
/// <param name="_logger">The logger.</param>
public class TestDataGenerator(TraditionalDbContext _traditionalDbContext, ILogger _logger)
{
    /// <summary>
    /// Sets up the example data asynchronously.
    /// </summary>
    /// <param name="customDataCount">The custom data count.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetupExampleDataAsync(int? customDataCount = null, CancellationToken cancellationToken = default)
    {
        const int defaultDataCount = 10_000;
        int dataCount = customDataCount ?? defaultDataCount;

        await CreateDatabaseAsync(cancellationToken);

        if (File.Exists(GetLocalDumpFilePathWithName(dataCount)))
        {
            await RestoreDump(GetDockerDumpFilePathWithName(dataCount), cancellationToken);
            _logger.Information("Restored dump for {DataCount} articles", dataCount);
            return;
        }

        // To simplify the update attribute values test are all values of type boolean
        const AttributeValueType valueType = AttributeValueType.Boolean;
#pragma warning disable S125 // Before we had: var valueType = (AttributeValueType)(i % 4);
#pragma warning restore S125

        _logger.Information("Start creating example data for {DataCount} articles", dataCount);
        var stopwatch = Stopwatch.StartNew();

        // 1. Get the german root Category, because we will later link all categories and attributes to it
        var germanRootCategory = await _traditionalDbContext.RootCategories.SingleAsync(x => x.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID, cancellationToken);

        // 2. Create data count articles
        var articles = ArticleFactory.CreateArticles(dataCount).ToArray();

        int uniqueCategoryNumber = 0;
        int uniqueAttributeNumber = 0;
        var categories = new List<Category>(dataCount * 3);
        var attributes = new List<Attribute>(dataCount * 9);
        List<Article> allArticles = [.. articles];
        for (int i = 0; i < articles.Length; i++)
        {
            // Get the current article
            var article = articles[i];

            // Create a category tree with a depth of 3 for each article
            var (baseCategory, childCategory, secondChildCategory) = CreateCategories(germanRootCategory, ref uniqueCategoryNumber);

            // Assign the categories to the article 1:1:1
            int articleToCategory = i % 3;
            article.Categories = articleToCategory switch
            {
                0 => [baseCategory],
                1 => [childCategory],
                _ => [secondChildCategory]
            };

            // Create 2 variants for each article
            var variants = ArticleFactory.CreateVariants(amount: 2, article.ArticleNumber, article.Categories).ToArray();
            allArticles.AddRange(variants);
            categories.AddRange([baseCategory, childCategory, secondChildCategory]);

            // Create attributes for each category with a depth of 3
            attributes.AddRange(CreateAttributes(
                valueType,
                germanRootCategory,
                [.. variants, article],
                baseCategory,
                childCategory,
                secondChildCategory,
                ref uniqueAttributeNumber));
        }

        _logger.Information("Finished creating example data for {DataCount} articles in {ElapsedMilliseconds} ms", dataCount, stopwatch.ElapsedMilliseconds);

        await _traditionalDbContext.Articles.AddRangeAsync(allArticles, cancellationToken);
        await _traditionalDbContext.Categories.AddRangeAsync(categories, cancellationToken);
        await _traditionalDbContext.Attributes.AddRangeAsync(attributes, cancellationToken);

        var changes = await _traditionalDbContext.SaveChangesAsync(cancellationToken);
        _traditionalDbContext.ChangeTracker.Clear();

        await CreateDump(GetDockerDumpFilePathWithName(dataCount), cancellationToken);

        _logger.Information("Saved {ChangesCount} changes and cleared the change tracker after total elapsed time of {ElapsedSeconds} s", changes, stopwatch.Elapsed.Seconds);
    }

    private static string GetLocalDumpFilePathWithName(int dataCount)
    {
        return Path.Combine(
            CommonDirectoryPath.GetSolutionDirectory().DirectoryPath,
            "data/postgres/dumps/",
            $"K6TestData_{dataCount.ToString(CultureInfo.InvariantCulture)}_Dump.sql");
    }

    private static string GetDockerDumpFilePathWithName(int dataCount)
    {
        return $"/dumps/K6TestData_{dataCount.ToString(CultureInfo.InvariantCulture)}_Dump.sql";
    }

    private static (Category baseCategory, Category childCategory, Category secondChildCategory) CreateCategories(RootCategory germanRootCategory, ref int uniqueCategoryNumber)
    {
        var baseCategory = CategoryFactory.CreateCategory(
            categoryNumber: ++uniqueCategoryNumber,
            name: $"Category Name {uniqueCategoryNumber}",
            path: $"Category Path {uniqueCategoryNumber}",
            rootCategory: germanRootCategory);

        var childCategory = CategoryFactory.CreateCategory(
            categoryNumber: ++uniqueCategoryNumber,
            name: $"Category Name {uniqueCategoryNumber}",
            path: $"Category Path {uniqueCategoryNumber}",
            isLeaf: true,
            rootCategory: germanRootCategory,
            parent: baseCategory);

        var secondChildCategory = CategoryFactory.CreateCategory(
            categoryNumber: ++uniqueCategoryNumber,
            name: $"Category Name {uniqueCategoryNumber}",
            path: $"Category Path {uniqueCategoryNumber}",
            isLeaf: true,
            rootCategory: germanRootCategory,
            parent: childCategory);

        return (baseCategory, childCategory, secondChildCategory);
    }

    private static Attribute[] CreateAttributes(
            AttributeValueType valueType,
            RootCategory germanRootCategory,
            IReadOnlyList<Article> articles,
            Category baseCategory,
            Category childCategory,
            Category secondChildCategory,
            ref int uniqueAttributeNumber)
    {
        var baseAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            rootCategory: germanRootCategory,
            categories: [baseCategory]);

        var firstUniqueAttributeNumber = uniqueAttributeNumber;

        var subAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            parentAttribute: baseAttribute,
            rootCategory: germanRootCategory,
            categories: [baseCategory]);

        var leafAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            parentAttribute: subAttribute,
            rootCategory: germanRootCategory,
            categories: [baseCategory]);

        var childAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            rootCategory: germanRootCategory,
            categories: [childCategory]);

        var subChildAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            parentAttribute: childAttribute,
            rootCategory: germanRootCategory,
            categories: [childCategory]);

        var leafChildAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            parentAttribute: subChildAttribute,
            rootCategory: germanRootCategory,
            categories: [childCategory]);

        var secondChildAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            rootCategory: germanRootCategory,
            categories: [secondChildCategory]);

        var subSecondChildAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            parentAttribute: secondChildAttribute,
            rootCategory: germanRootCategory,
            categories: [secondChildCategory]);

        var leafSecondChildAttribute = AttributeFactory.CreateAttribute(
            name: $"Attribute Name {++uniqueAttributeNumber}",
            valueType: valueType,
            marketplaceAttributeIds: $"Marketplace Attribute Id {uniqueAttributeNumber}",
            parentAttribute: subSecondChildAttribute,
            rootCategory: germanRootCategory,
            categories: [secondChildCategory]);

        var mappedCategory = articles[0].Categories!.Single();
        if (mappedCategory == baseCategory)
        {
            foreach (var article in articles)
            {
                AttributeFactory.AddValueWithNumberToAttribute(baseAttribute, firstUniqueAttributeNumber++, article);
                AttributeFactory.AddValueWithNumberToAttribute(subAttribute, firstUniqueAttributeNumber++, article);
                AttributeFactory.AddValueWithNumberToAttribute(leafAttribute, firstUniqueAttributeNumber++, article);
            }
        }
        else if (mappedCategory == childCategory)
        {
            foreach (var article in articles)
            {
                AttributeFactory.AddValueWithNumberToAttribute(childAttribute, firstUniqueAttributeNumber++, article);
                AttributeFactory.AddValueWithNumberToAttribute(subChildAttribute, firstUniqueAttributeNumber++, article);
                AttributeFactory.AddValueWithNumberToAttribute(leafChildAttribute, firstUniqueAttributeNumber++, article);
            }
        }
        else if (mappedCategory == secondChildCategory)
        {
            foreach (var article in articles)
            {
                AttributeFactory.AddValueWithNumberToAttribute(secondChildAttribute, firstUniqueAttributeNumber++, article);
                AttributeFactory.AddValueWithNumberToAttribute(subSecondChildAttribute, firstUniqueAttributeNumber++, article);
                AttributeFactory.AddValueWithNumberToAttribute(leafSecondChildAttribute, firstUniqueAttributeNumber, article);
            }
        }

        return
        [
            baseAttribute,
            subAttribute,
            leafAttribute,
            childAttribute,
            subChildAttribute,
            leafChildAttribute,
            secondChildAttribute,
            subSecondChildAttribute,
            leafSecondChildAttribute
        ];
    }

    private async Task CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        await _traditionalDbContext.Database.EnsureDeletedAsync(cancellationToken);
        await _traditionalDbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private async Task CreateDump(string dockerDumpFilePath, CancellationToken cancellationToken)
    {
        string dumpCommand = $"docker exec db-main-postgres pg_dump -U postgres-user -d postgres-main -f {dockerDumpFilePath}";
        await ExecuteShellCommand(dumpCommand, cancellationToken);
    }

    private async Task RestoreDump(string dockerDumpFilePath, CancellationToken cancellationToken)
    {
        await _traditionalDbContext.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;", cancellationToken);

        string restoreCommand = $"docker exec db-main-postgres psql -U postgres-user -d postgres-main -f {dockerDumpFilePath}";
        await ExecuteShellCommand(restoreCommand, cancellationToken);
    }

    private async Task ExecuteShellCommand(string command, CancellationToken cancellationToken)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processInfo;

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command failed with exit code {process.ExitCode}: {error}");
        }

        _logger.Information("Command output:\n{Output}", output.Split('\n').Select(x => x.TrimEnd('\r')).ToArray());
    }
}
