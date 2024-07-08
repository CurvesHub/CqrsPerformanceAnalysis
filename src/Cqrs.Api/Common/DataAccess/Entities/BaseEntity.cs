namespace Cqrs.Api.Common.DataAccess.Entities;

/// <summary>
/// Base class for all entities.
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public int Id { get; init; }
}
