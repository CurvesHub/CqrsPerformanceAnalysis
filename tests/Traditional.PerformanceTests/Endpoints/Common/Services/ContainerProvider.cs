using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace Traditional.PerformanceTests.Endpoints.Common.Services;

/// <summary>
/// Provides the containers for the automated tests.
/// </summary>
public class ContainerProvider
{
    private const long LIMIT_TO_ONE_CPU = 1000000000;
    private const long LIMIT_TO_ONE_GB_OF_MEMORY = 1073741824;

    private PostgreSqlContainer? _dbContainer;
    private IContainer? _apiContainer;
    private IContainer? _k6Container;

    /// <summary>
    /// Stops and removes the containers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task CleanupAsync()
    {
        var cancellationToken = CancellationToken.None;
        if (_k6Container is not null)
        {
            await _k6Container.StopAsync(cancellationToken);
            await _k6Container.DisposeAsync();
        }

        if (_apiContainer is not null)
        {
            await _apiContainer.StopAsync(cancellationToken);
            await _apiContainer.DisposeAsync();
        }

        if (_dbContainer is not null)
        {
            await _dbContainer.StopAsync(cancellationToken);
            await _dbContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Starts the api container.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartApiContainerAsync(CancellationToken cancellationToken = default)
    {
        _apiContainer = BuildApiContainer();
        await _apiContainer.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Starts the db container.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartDbContainerAsync(CancellationToken cancellationToken = default)
    {
        _dbContainer = BuildDbContainer();
        await _dbContainer.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Starts the k6 test, waits for it to finish and gets the exit code and the logs.
    /// </summary>
    /// <param name="testDirectoryName">The name of the test directory.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code and the logs of the k6 test.</returns>
    public async Task<(long exitCode, (string Stdout, string Stderr) logs)> StartK6TestAndGetK6ExitCodeAndLogsAsync(
        string testDirectoryName,
        string endpointName,
        CancellationToken cancellationToken = default)
    {
        _k6Container = BuildK6Container(testDirectoryName, endpointName);

        // Start the k6 container and the test.
        await _k6Container.StartAsync(cancellationToken);

        // This call will wait for the test to finish and the container to stop to get the exit code.
        var exitCode = await _k6Container.GetExitCodeAsync(cancellationToken);

        return (exitCode, await _k6Container.GetLogsAsync(ct: cancellationToken));
    }

    private static PostgreSqlContainer BuildDbContainer()
    {
        return new PostgreSqlBuilder()
            .WithImage("postgres:16.2")
            .WithNetwork("traditional-network")
            .WithName("db-traditional-postgres")
            .WithDatabase("postgres-traditional")
            .WithUsername("postgres-user")
            .WithPassword("postgres-password")
            .WithPortBinding(5432)
            .WithBindMount(Path.Combine(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, "data/postgres/dumps"), "/dumps")
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameterModifier =>
            {
                // Resource limits: 1 CPU and 1 GB of memory.
                parameterModifier.HostConfig.NanoCPUs = LIMIT_TO_ONE_CPU;
                parameterModifier.HostConfig.Memory = LIMIT_TO_ONE_GB_OF_MEMORY;
            })
            .Build();
    }

    private static IContainer BuildApiContainer()
    {
#pragma warning disable S125 // Sections of code should not be commented out
        // Optional: Build the api image new very time to ensure that the latest changes are used.
        // Currently, the image is build manuel once in a while, therefore the latest changes are not always used.
        // Note: It's not as simply as that below.
        // The api would not start since it would not be able to access the app settings file.
        // Since the complete src dir is copied to the image, the app settings file can't be missing.
        // Also, when building the image manuel it works just fine
        /*var apiImage = new ImageFromDockerfileBuilder()
            .WithDeleteIfExists(true)
            .WithDockerfileDirectory(CommonDirectoryPath.GetProjectDirectory().DirectoryPath)
            .WithDockerfile("Dockerfile-Traditional")
            .WithName("api.traditional")
            .Build();
        await apiImage.CreateAsync();*/
#pragma warning restore S125

        return new ContainerBuilder()
            .WithImage("api.traditional:latest")
            .WithNetwork("traditional-network")
            .WithName("api-traditional")
            .WithPortBinding(5012, 8080)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameterModifier =>
            {
                // Resource limits: 1 CPU and 1 GB of memory.
                parameterModifier.HostConfig.NanoCPUs = LIMIT_TO_ONE_CPU;
                parameterModifier.HostConfig.Memory = LIMIT_TO_ONE_GB_OF_MEMORY;
            })
            .Build();
    }

    private static IContainer BuildK6Container(string testDirectoryName, string endpointName)
    {
        var testDirectory = Path.GetFullPath(Path.Combine(
            CommonDirectoryPath.GetProjectDirectory().DirectoryPath,
            "Assets/K6Tests",
            testDirectoryName));

        string[] commands =
        [
            "run",
            $"/scripts/{testDirectoryName}/K6-{endpointName}.js",
            $"--summary-export=/results/{testDirectoryName}/K6-{endpointName}-Summary.json",
            "--out",
            $"json=/results/{testDirectoryName}/K6-{endpointName}-Metric.jsonl"
        ];

        return new ContainerBuilder()
            .WithImage("grafana/k6")
            .WithNetwork("traditional-network")
            .WithName("k6-traditional")
            .WithBindMount(Path.Combine(testDirectory, "scripts"), $"/scripts/{testDirectoryName}")
            .WithBindMount(Path.Combine(testDirectory, "results"), $"/results/{testDirectoryName}")
            .WithCommand(commands)
            .WithEnvironment("K6_WEB_DASHBOARD", "true")
            .WithEnvironment("K6_WEB_DASHBOARD_EXPORT", $"/results/{testDirectoryName}/K6-{endpointName}-Report.html")
            .WithPortBinding(5665)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameterModifier =>
            {
                // Resource limits: 2 CPU and 2 GB of memory.
                parameterModifier.HostConfig.NanoCPUs = LIMIT_TO_ONE_CPU * 2;
                parameterModifier.HostConfig.Memory = LIMIT_TO_ONE_GB_OF_MEMORY * 2;
            })
            .Build();
    }
}
