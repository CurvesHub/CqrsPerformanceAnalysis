using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace PerformanceTests.Common.Constants;

/// <summary>
/// Provides the available API names for testing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
[PublicAPI]
public enum AvailableApiNames
{
    /// <summary>
    /// The name for the traditional API.
    /// </summary>
    TraditionalApi,

    /// <summary>
    /// The name for the cqrs API.
    /// </summary>
    CqrsApi,

    /// <summary>
    /// The name for the cqrs API using MediatR.
    /// </summary>
    CqrsApiMediatr,

    /// <summary>
    /// The name for all APIs.
    /// </summary>
    AllApis
}
