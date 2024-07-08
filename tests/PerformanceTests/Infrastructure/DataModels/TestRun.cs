namespace PerformanceTests.Infrastructure.DataModels;

/// <summary>
/// Encapsulates a test run.
/// </summary>
/// <param name="TestName">The name of the test.</param>
/// <param name="SummaryContent">The summary json content of the test run.</param>
/// <param name="HtmlReport">The html report of the test run.</param>
/// <param name="CreatedAt">The creation time in utc of the test run.</param>
/// <param name="LogsStdout">The standard output of the test run.</param>
/// <param name="LogsStderr">The standard error of the test run.</param>
/// <param name="Metadata">The metadata associated with the test run.</param>
public record TestRun(
    string TestName,
    string SummaryContent,
    string HtmlReport,
    DateTimeOffset CreatedAt,
    string? LogsStdout = null,
    string? LogsStderr = null,
    string? Metadata = null)
{
    /// <summary>
    /// Gets the id of the test run.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the data points for the test run.
    /// </summary>
    public List<DataPoint>? DataPoints { get; init; }
}
