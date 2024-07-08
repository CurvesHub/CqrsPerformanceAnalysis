using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;

namespace Cqrs.Tests.TestCommon.JsonConverter;

/// <summary>
/// A custom JSON converter for <see cref="ErrorOr.Error"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request in the metadata dictionary.</typeparam>
public class ErrorJsonConverter<TRequest> : JsonConverter<Error>
{
    /// <inheritdoc/>
    public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonOptions = new JsonSerializerOptions(options)
        {
            PropertyNameCaseInsensitive = true
        };

        if (!jsonOptions.Converters.Any(c => c is JsonStringEnumConverter))
        {
            jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject but got {reader.TokenType}");
        }

        string? code = null;
        string? description = null;
        int? numericType = null;
        Dictionary<string, TRequest>? metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return GetError(code, description, numericType, metadata);
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName but got {reader.TokenType}");
            }

            var propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "code" or "Code":
                    code = reader.GetString();
                    break;
                case "description" or "Description":
                    description = reader.GetString();
                    break;
                case "type" or "Type":
                    break;
                case "numericType" or "NumericType":
                    numericType = reader.GetInt32();
                    break;
                case "metadata" or "Metadata":
                    metadata = JsonSerializer.Deserialize<Dictionary<string, TRequest>>(ref reader, jsonOptions);
                    break;
                default:
                    throw new JsonException($"Unexpected property '{propertyName}' found in JSON.");
            }
        }

        throw new JsonException("Unexpected error occurred while reading JSON.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options) => throw new NotSupportedException();

    private static Error GetError(
        string? code,
        string? description,
        int? numericType,
        Dictionary<string, TRequest>? metadata)
    {
        if (code is null || description is null || numericType is null)
        {
            throw new JsonException("At least one required property not found in JSON.");
        }

        Dictionary<string, object>? newMetadata = null;
        if (metadata is not null)
        {
            newMetadata = new Dictionary<string, object>(metadata.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in metadata)
            {
                newMetadata.Add(key, value!);
            }
        }

        return (ErrorType)numericType switch
        {
            ErrorType.Failure => Error.Failure(code, description, newMetadata),
            ErrorType.Unexpected => Error.Unexpected(code, description, newMetadata),
            ErrorType.Validation => Error.Validation(code, description, newMetadata),
            ErrorType.Conflict => Error.Conflict(code, description, newMetadata),
            ErrorType.NotFound => Error.NotFound(code, description, newMetadata),
            ErrorType.Unauthorized => Error.Unauthorized(code, description, newMetadata),
            ErrorType.Forbidden => Error.Forbidden(code, description, newMetadata),
            _ => Error.Custom(numericType.Value, code, description, newMetadata)
        };
    }
}
