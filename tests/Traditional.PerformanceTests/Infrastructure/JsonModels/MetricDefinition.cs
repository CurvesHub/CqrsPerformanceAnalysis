namespace Traditional.PerformanceTests.Infrastructure.JsonModels;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. -> Will be initialized during the serialization process.

/// <summary>
/// Encapsulates a metric definition.
/// </summary>
/// <param name="Type">The type of the metric.</param>
/// <param name="Metric">The name of the metric.</param>
public record MetricDefinition(string Type, string Metric)
{
    /// <summary>
    /// Gets the data for the metric.
    /// </summary>
    public MetricData Data { get; init; }

    /// <summary>
    /// Encapsulates the data for a metric.
    /// </summary>
    /// <param name="Name">The name of the metric.</param>
    /// <param name="Type">The data type.</param>
    /// <param name="Contains">The data type that the metric contains.</param>
    /// <param name="Thresholds">A list of thresholds for the metric.</param>
    public record MetricData(
        string Name,
        string Type,
        string Contains,
        List<string> Thresholds);
}
