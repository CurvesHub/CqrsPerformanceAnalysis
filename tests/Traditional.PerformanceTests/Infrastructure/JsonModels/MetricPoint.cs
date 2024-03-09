namespace Traditional.PerformanceTests.Infrastructure.JsonModels;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. -> Will be initialized during the serialization process.

/// <summary>
/// Encapsulates a metric point.
/// </summary>
/// <param name="Metric">The name of the metric.</param>
/// <param name="Type">The type of the metric.</param>
public record MetricPoint(string Metric, string Type)
{
    /// <summary>
    /// Gets the data for the metric.
    /// </summary>
    public PointData Data { get; init; }

    /// <summary>
    /// Encapsulates the data for a metric point.
    /// </summary>
    /// <param name="Time">The time that the data point was recorded.</param>
    /// <param name="Value">The value of the data point.</param>
    /// <param name="Tags">The tags associated with the data point.</param>
    public record PointData(DateTimeOffset Time, double Value, Dictionary<string, string> Tags);
}
