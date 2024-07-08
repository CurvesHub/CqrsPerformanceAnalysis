using Cqrs.Api.Common.DataAccess.Persistence;

namespace Cqrs.Tests.TestCommon.BaseTest;

/// <summary>
/// The base test class to inherit from when using a <see cref="TraditionalApiFactory"/>.
/// </summary>
[Collection(nameof(SharedTraditionalApiFactoryTestCollection))]
public class BaseTestWithSharedTraditionalApiFactory : IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase;
    private readonly Func<Task> _resetCache;
    private readonly Func<TraditionalDbContext> _resolveTraditionalDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTestWithSharedTraditionalApiFactory"/> class.
    /// </summary>
    /// <param name="factory">The <see cref="TraditionalApiFactory"/>. It will be provided by xUnit.</param>
    protected BaseTestWithSharedTraditionalApiFactory(TraditionalApiFactory factory)
    {
        Services = factory.Services;
        HttpClient = factory.HttpClient;
        _resetDatabase = () => factory.ResetDatabaseAsync(withReseed: true);
        _resetCache = factory.ResetMemoryCacheAsync;
        _resolveTraditionalDbContext = factory.ResolveTraditionalDbContext;
    }

    /// <summary>
    /// Gets the pre-configured <see cref="HttpClient"/>.
    /// </summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// Gets a provider for accessing the service container.
    /// </summary>
    protected IServiceProvider Services { get; }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <returns>A <see cref="Task.CompletedTask"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync() => await Task.CompletedTask;

    /// <summary>
    /// Resets the database.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        await Task.WhenAll(_resetDatabase(), _resetCache());
    }

    /// <summary>
    /// Gets the pre-configured <see cref="TraditionalDbContext"/>.
    /// </summary>
    /// <returns>The newly created traditional database context.</returns>
    protected TraditionalDbContext ResolveTraditionalDbContext() => _resolveTraditionalDbContext();
}
