using Traditional.PerformanceTests.Endpoints.Common.Models;
using Traditional.PerformanceTests.Infrastructure;
using ILogger = Serilog.ILogger;

namespace Traditional.PerformanceTests.Endpoints.Common.Services;

/// <summary>
/// Provides functionality to handle the automated k6 test for a specific endpoint.
/// </summary>
public class K6TestHandler(
    ILogger _logger,
    PerformanceDbContext _performanceDbContext,
    ContainerProvider _containerProvider,
    TestDataGenerator _testDataGenerator,
    ApiClient _apiClient,
    ResultProcessor _resultProcessor)
{
    /// <summary>
    /// Handles an automated k6 test for an endpoint.
    /// </summary>
    /// <param name="testInformation">The test information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartK6TestAndProcessResultsAsync(
        TestInformation testInformation,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Preparing the K6 test for: {EndpointName}", testInformation.EndpointName);

        try
        {
            await ThrowIfRequiredContainersAreNotRunningAsync(testInformation.CheckElastic, cancellationToken);

            _logger.Information("The required messuring/result containers are running. Starting the api and database containers");

            await _containerProvider.StartApiContainerAsync(cancellationToken);
            await _containerProvider.StartDbContainerAsync(cancellationToken);

            _logger.Information("Creating the database and setting up the example data");

            await _testDataGenerator.SetupExampleDataAsync(cancellationToken: cancellationToken);

            await _apiClient.WaitForApiToBeReady(cancellationToken);

            if (testInformation.WithWarmUp)
            {
                await _apiClient.SendWarmupRequestsAsync(
                    route: testInformation.EndpointRoute,
                    request: testInformation.WarmUpRequest,
                    cancellationToken: cancellationToken);
                _logger.Information("Api is warmed up");
            }

            _logger.Information("Starting the k6 test and waiting for it to finish");

            var (exitCode, logs) = await _containerProvider
                .StartK6TestAndGetK6ExitCodeAndLogsAsync(
                    testDirectoryName: testInformation.TestDirectoryName,
                    endpointName: testInformation.EndpointName,
                    cancellationToken: cancellationToken);

            _logger.Information("The k6 test is stopped with exit code {ExitCode}. Processing the results", exitCode);

            await _resultProcessor.ProcessResultsAsync(logs, testInformation.SaveMinimalResults, cancellationToken);
        }
        finally
        {
            await _containerProvider.CleanupAsync();
            _logger.Information("Finished K6 test for: {EndpointName}. The containers are stopped, disposed and being deleted", testInformation.EndpointName);
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the required containers are not running.
    /// </summary>
    /// <param name="checkElastic">Whether to check if the elastic search container is running.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if the required containers are not running.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task ThrowIfRequiredContainersAreNotRunningAsync(
        bool checkElastic,
        CancellationToken cancellationToken = default)
    {
        if (!await _performanceDbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("The performance db is not running.");
        }

        if (checkElastic)
        {
            var client = new HttpClient();

            var elasticResponse = await client.GetAsync("http://localhost:9200", cancellationToken);
            if (!elasticResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Elastic search is not running.");
            }
        }
    }
}
