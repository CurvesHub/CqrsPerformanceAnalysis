namespace Traditional.PerformanceTests.Infrastructure.DataModels;

/// <summary>
/// Encapsulates a data point for a metric.
/// </summary>
/// <param name="Timestamp">The timestamp of the data point.</param>
/// <param name="MetricName">The name of the metric.</param>
/// <param name="Value">The value of the data point.</param>
/// <param name="Tags">The tags associated with the data point.</param>
public record DataPoint(
    DateTimeOffset Timestamp,
    string MetricName,
    double Value,
    Dictionary<string, string> Tags)
{
    /// <summary>
    /// Gets the id of the data point.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///  Gets or sets the type of the metric.
    /// </summary>
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the metric.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the id of the test run that the data point belongs to.
    /// </summary>
    public int TestRunId { get; init; }

    /// <summary>
    /// Gets the test run that the data point belongs to.
    /// </summary>
    public TestRun? TestRun { get; init; }
}
