using System.Runtime.CompilerServices;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Traditional.PerformanceTests.Infrastructure;
using Traditional.PerformanceTests.Infrastructure.DataModels;
using Traditional.PerformanceTests.Infrastructure.JsonModels;
using ILogger = Serilog.ILogger;

namespace Traditional.PerformanceTests.Endpoints.Common.Services;

/// <summary>
/// Provides functionality to process the results of a k6 test.
/// </summary>
/// <param name="_logger">The logger.</param>
/// <param name="_performanceDbContext">The performance database context.</param>
public class ResultProcessor(ILogger _logger, PerformanceDbContext _performanceDbContext)
{
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Processes the results of a k6 test.
    /// </summary>
    /// <param name="logs">The logs of the k6 test.</param>
    /// <param name="saveMinimalResults">A value indicating whether to save minimal results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ProcessResultsAsync((string Stdout, string Stderr)? logs = null, bool saveMinimalResults = true, CancellationToken cancellationToken = default)
    {
        _logger.Information("Started processing K6 Results");

        // Get all directories with the name "results" in the "Assets/K6Tests" directory
        var resultDirectories = Directory.GetDirectories(
            Path.GetFullPath(Path.Combine(CommonDirectoryPath.GetProjectDirectory().DirectoryPath, "Assets/K6Tests")),
            "results",
            SearchOption.AllDirectories);

        // For each result directory, process the files and add the test run to the database
        var resultTasks = resultDirectories
            .Select(dir => ProcessResultDirectoryAsync(dir, logs, saveMinimalResults, cancellationToken).ToArrayAsync(cancellationToken).AsTask());

        var resultPerDir = (await Task.WhenAll(resultTasks)).SelectMany(test => test);

        var testRuns = resultPerDir
            .Where(testRun => testRun is not null)
            .Select(testRun => testRun!)
            .ToArray();

        await _performanceDbContext.TestRuns.AddRangeAsync(testRuns);
        var changes = await _performanceDbContext.SaveChangesAsync(cancellationToken);
        _performanceDbContext.ChangeTracker.Clear();

        _logger.Information(
            "Saved {Changes} changes! TestRuns: {TestRunCount} and DataPoints: {DataPointCount}",
            changes,
            testRuns.Length,
            testRuns.Sum(testRun => testRun.DataPoints?.Count));
    }

    private static string GetTestName(IEnumerable<string> filePaths)
    {
        return Path
            .GetFileNameWithoutExtension(filePaths.Single(path => path.Contains("Summary", StringComparison.Ordinal)))
            .Replace("-Summary", string.Empty, StringComparison.Ordinal);
    }

    private static DateTime GetCreationDateTimeWithoutSeconds(string filePath)
    {
        var creationTime = File.GetCreationTime(filePath);
        return new DateTime(
            creationTime.Year,
            creationTime.Month,
            creationTime.Day,
            creationTime.Hour,
            creationTime.Minute,
            second: 0,
            creationTime.Kind);
    }

    private async IAsyncEnumerable<TestRun?> ProcessResultDirectoryAsync(
        string directory,
        (string Stdout, string Stderr)? logs,
        bool saveMinimalResults,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get all files in the directory
        var allFiles = Directory.GetFiles(directory);

        if (allFiles.Length == 0)
        {
            _logger.Information("No files found skipping directory: {Directory}", directory);
            yield return null;
        }

        foreach (var (key, value) in GroupedFiles(allFiles))
        {
            yield return await ConstructTestRun(logs, value, key, saveMinimalResults, cancellationToken);
        }
    }

    private IEnumerable<(DateTime creationTime, string[] testRunFiles)> GroupedFiles(string[] allFiles)
    {
        // Try to group by names
        var groupedByName = allFiles
            .GroupBy(s => Path.GetFileName(s).Split('-')[1], StringComparer.Ordinal)
            .ToList();

        // Check if each group has 3 files
        var groupsWithNotExactly3Files = groupedByName
            .Where(group => group.Take(3).Count() != 3)
            .ToList();

        if (groupsWithNotExactly3Files.Count == 0)
        {
            return groupedByName.Select(group => (
                GetCreationDateTimeWithoutSeconds(group.First()),
                group.ToArray()));
        }

        // At this point we know that there are multiple test runs for the same test.
        // We take the group which has more than 3 files and try to group by the creation date time without seconds and check again if each group has 3 files.
        var groupedByCreationTime = groupsWithNotExactly3Files
            .SelectMany(group => group)
            .GroupBy(GetCreationDateTimeWithoutSeconds)
            .ToList();

        var groupsWithNotExactly3FilesAfterSecondGrouping = groupedByCreationTime
            .Where(group => group.Take(3).Count() != 3)
            .ToList();

        if (groupsWithNotExactly3FilesAfterSecondGrouping.Count == 0)
        {
            return groupedByCreationTime.Select(group => (group.Key, group.ToArray()));
        }

        // At this point we know that there are multiple test runs for the same test in allFiles
        // And grouping by creation time without seconds resulted in multiple groups with more than 3 files
        // Its possible that the second grouping by time did not work fully because if the k6 dashboard is still opened the html report file is created later as the other files
        // So we filter those results out and return the successfully grouped files
        // But we also archive the files which are not grouped correctly, so they don't loop again through the process
        ArchiveFiles(groupsWithNotExactly3FilesAfterSecondGrouping.SelectMany(group => group).ToArray(), DateTime.Now);

        return groupedByCreationTime
            .Except(groupsWithNotExactly3FilesAfterSecondGrouping)
            .Select(group => (group.Key, group.ToArray()));
    }

    private async Task<TestRun?> ConstructTestRun(
        (string Stdout, string Stderr)? logs,
        IReadOnlyCollection<string> groupedFiles,
        DateTime creationTime,
        bool saveMinimalResults,
        CancellationToken cancellationToken = default)
    {
        if (groupedFiles.Count is not 3)
        {
            _logger.Error("Expected 3 files in the directory, but found {FileCount}", groupedFiles.Count);
            throw new InvalidOperationException("Expected 3 files in the directory");
        }

        string summaryContent = string.Empty;
        string htmlReport = string.Empty;
        List<DataPoint> dataPoints = [];
        foreach (var file in groupedFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            if (file.Contains("Summary", StringComparison.Ordinal))
            {
                summaryContent = await File.ReadAllTextAsync(file, cancellationToken);
            }
            else if (file.Contains("Metric", StringComparison.Ordinal))
            {
                dataPoints = await ExtractDataPointsAsync(file, saveMinimalResults, cancellationToken);
            }
            else if (file.Contains("Report", StringComparison.Ordinal))
            {
                htmlReport = await File.ReadAllTextAsync(file, cancellationToken);
            }
        }

        ArchiveFiles(groupedFiles, creationTime);

        return new TestRun(
            TestName: GetTestName(groupedFiles),
            SummaryContent: summaryContent,
            HtmlReport: htmlReport,
            CreatedAt: new DateTimeOffset(creationTime).ToUniversalTime(),
            LogsStdout: logs?.Stdout,
            LogsStderr: logs?.Stderr)
        {
            DataPoints = dataPoints
        };
    }

    private async Task<List<DataPoint>> ExtractDataPointsAsync(string filePath, bool saveMinimalResults, CancellationToken cancellationToken = default)
    {
        List<DataPoint> dataPoints = [];
        List<MetricDefinition> metricDefinitions = [];

        await foreach (var line in File.ReadLinesAsync(filePath, cancellationToken))
        {
            if (line.Contains("\"type\":\"Metric\"", StringComparison.Ordinal))
            {
                var metricDefinition = JsonSerializer.Deserialize<MetricDefinition>(line, _serializerOptions);
                if (metricDefinition is not null)
                {
                    metricDefinitions.Add(metricDefinition);
                }

                continue;
            }

            if (line.Contains("\"type\":\"Point\"", StringComparison.Ordinal))
            {
                if (saveMinimalResults
                    && !line.Contains("\"metric\":\"vus\"", StringComparison.Ordinal)
                    && !line.Contains("\"metric\":\"http_req_duration\"", StringComparison.Ordinal))
                {
                    continue;
                }

                var metricPoint = JsonSerializer.Deserialize<MetricPoint>(line, _serializerOptions);
                if (metricPoint is not null)
                {
                    dataPoints.Add(new DataPoint(
                        Timestamp: metricPoint.Data.Time,
                        MetricName: metricPoint.Metric,
                        Value: metricPoint.Data.Value,
                        Tags: metricPoint.Data.Tags));
                }
            }
            else
            {
                _logger.Error("Could not deserialize metric file line: {Line}", line);
            }
        }

        foreach (var dataPoint in dataPoints)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            var matchingMetricDefinition = metricDefinitions.Single(definition =>
                string.Equals(definition.Metric, dataPoint.MetricName, StringComparison.Ordinal));

            dataPoint.MetricType = matchingMetricDefinition.Data.Type;
            dataPoint.DataType = matchingMetricDefinition.Data.Contains;
        }

        return dataPoints;
    }

    private void ArchiveFiles(IReadOnlyCollection<string> filePaths, DateTime creationTime)
    {
        foreach (var filePath in filePaths)
        {
            var archivePath = Path.Combine(Path.GetDirectoryName(filePath)!, "../archive");
            Directory.CreateDirectory(archivePath);

            if (filePath.Contains("Report", StringComparison.Ordinal))
            {
                archivePath = Path.Combine(archivePath, "report");
                Directory.CreateDirectory(archivePath);
            }

            var newFileName = $"{creationTime:yyyy-MM-dd_HH-mm-ss}_{Path.GetFileName(filePath)}";
            var destFileName = Path.Combine(archivePath, newFileName);

            File.Move(filePath, destFileName);
        }

        _logger.Information("Moved {FileCount} files to the 'archive' folders", filePaths.Count);
    }
}
