namespace Astrolabed.Api.Converters;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Converts <see cref="IPAddress"/> objects to and from JSON string representations.
/// </summary>
public sealed class IPAddressJsonConverter : JsonConverter<IPAddress>
{
    /// <inheritdoc />
    public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token when deserializing IPAddress, but found '{reader.TokenType}'.");
        }

        string? ipString = reader.GetString();

        if (string.IsNullOrWhiteSpace(ipString))
        {
            return null;
        }

        if (IPAddress.TryParse(ipString, out IPAddress? parsedIp))
        {
            return parsedIp;
        }

        throw new JsonException($"Unable to parse '{ipString}' as a valid IPv4 or IPv6 address.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
