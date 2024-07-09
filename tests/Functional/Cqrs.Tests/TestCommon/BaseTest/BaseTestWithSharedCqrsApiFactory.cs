using Cqrs.Api.Common.DataAccess.Persistence;

namespace Cqrs.Tests.TestCommon.BaseTest;

/// <summary>
/// The base test class to inherit from when using a <see cref="CqrsApiFactory"/>.
/// </summary>
[Collection(nameof(SharedCqrsApiFactoryTestCollection))]
public class BaseTestWithSharedCqrsApiFactory : IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase;
    private readonly Func<Task> _resetCache;
    private readonly Func<CqrsWriteDbContext> _resolveCqrsWriteDbContext;
    private readonly Func<CqrsReadDbContext> _resolveCqrsReadDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTestWithSharedCqrsApiFactory"/> class.
    /// </summary>
    /// <param name="factory">The <see cref="CqrsApiFactory"/>. It will be provided by xUnit.</param>
    protected BaseTestWithSharedCqrsApiFactory(CqrsApiFactory factory)
    {
        Services = factory.Services;
        HttpClient = factory.HttpClient;
        _resetDatabase = () => factory.ResetDatabaseAsync(withReseed: true);
        _resetCache = factory.ResetMemoryCacheAsync;
        _resolveCqrsWriteDbContext = factory.ResolveCqrsWriteDbContext;
        _resolveCqrsReadDbContext = factory.ResolveCqrsReadDbContext;
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
    /// Gets the pre-configured <see cref="CqrsWriteDbContext"/>.
    /// </summary>
    /// <returns>The newly created cqrs write database context.</returns>
    protected CqrsWriteDbContext ResolveCqrsWriteDbContext() => _resolveCqrsWriteDbContext();

    /// <summary>
    /// Gets the pre-configured <see cref="CqrsReadDbContext"/>.
    /// </summary>
    /// <returns>The newly created cqrs read database context.</returns>
    protected CqrsReadDbContext ResolveCqrsReadDbContext() => _resolveCqrsReadDbContext();
}
