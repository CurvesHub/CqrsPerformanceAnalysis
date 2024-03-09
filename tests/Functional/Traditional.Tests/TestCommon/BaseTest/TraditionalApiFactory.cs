using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Configuration;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Configuration;

namespace Traditional.Tests.TestCommon.BaseTest;

/// <summary>
/// A custom WebApplicationFactory for the Traditional API in integration tests.
/// </summary>
[UsedImplicitly]
public class TraditionalApiFactory : WebApplicationFactory<Traditional.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16.2")
        .WithCleanUp(true)
        .Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _dbConnection = null!;

    /// <summary>
    /// Gets a pre-configured shared <see cref="HttpClient"/> for all tests.
    /// </summary>
    public HttpClient HttpClient { get; private set; } = null!;

    /// <summary>
    /// Initializes the postgres container, the http client and the database.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        await _dbContainer.StartAsync();
        HttpClient = CreateClient();
        await InitializeDatabaseAsync();
    }

    /// <summary>
    /// Stops and disposes the Postgres container.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public new async Task DisposeAsync()
    {
        await _dbConnection.CloseAsync();
        await _dbConnection.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Resets the database.
    /// </summary>
    /// <param name="withReseed">A bool indicating whether to reseed the database.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ResetDatabaseAsync(bool withReseed = true)
    {
        await _respawner.ResetAsync(_dbConnection);

        if (withReseed)
        {
            await SeedDataBase();
        }
    }

    /// <summary>
    /// Disposes the memory cache, so the singleton service is re-initialized and no state is shared between tests.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ResetMemoryCacheAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        // Optional: Improve this since we don't know how many keys are in the cache but we cant dispose it or clear it sadly
        // NOTE: We know that each key is the entity name of an entity inherited from BaseEntity
        memoryCache.Remove("RootCategory");
        memoryCache.Remove("AttributeMapping");
        memoryCache.Remove("AttributeCorrection");
    }

    /// <summary>
    /// Resolves a new instance of <see cref="TraditionalDbContext"/> from the service provider.
    /// </summary>
    /// <returns>A new instance of <see cref="TraditionalDbContext"/>.</returns>
    public TraditionalDbContext ResolveTraditionalDbContext()
    {
        return Services.CreateAsyncScope().ServiceProvider.GetRequiredService<TraditionalDbContext>();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TraditionalDbContext>>();
            services.RemoveAll<TraditionalDbContext>();

            services.AddDbContext<TraditionalDbContext>(options =>
                options.UseNpgsql(
                    connectionString: _dbContainer.GetConnectionString() + ";Include Error Detail=true",
                    npgsqlOptionsAction: config => config.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        });
    }

    private async Task InitializeDatabaseAsync()
    {
        await using (var dbContext = ResolveTraditionalDbContext())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            WithReseed = true
        });
    }

    private async Task SeedDataBase()
    {
        await using var dbContext = ResolveTraditionalDbContext();
        await dbContext.RootCategories.AddRangeAsync(RootCategoryConfigurations.GetSeedingData());
        await dbContext.AttributeMappings.AddRangeAsync(AttributeMappingConfigurations.GetSeedingData());
        await dbContext.SaveChangesAsync();
    }
}
