namespace Cqrs.Api.Common.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a seeded table is empty.
/// </summary>
/// <param name="tableName">Sets the table name indicating which table is empty.</param>
public sealed class SeededTableIsEmptyException(string tableName)
    : Exception($"Table {tableName}s is empty. Please seed the database.");
