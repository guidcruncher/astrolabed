using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabed.Serialization;

public sealed class PhysicalAddressJsonConverter : JsonConverter<PhysicalAddress>
{
    public override PhysicalAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : PhysicalAddress.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, PhysicalAddress value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var bytes = value.GetAddressBytes();
        var formattedMac = string.Join(":", bytes.Select(b => b.ToString("X2")));

        writer.WriteStringValue(formattedMac);
    }
}

