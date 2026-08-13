using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabed.Dhcp;

internal sealed class PhysicalAddressJsonConverter : JsonConverter<PhysicalAddress>
{
    public override PhysicalAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var macStr = reader.GetString();
        return string.IsNullOrWhiteSpace(macStr) ? PhysicalAddress.None : PhysicalAddress.Parse(macStr);
    }

    public override void Write(Utf8JsonWriter writer, PhysicalAddress value, JsonSerializerOptions options)
    {
        var bytes = value.GetAddressBytes();
        writer.WriteStringValue(string.Join(":", bytes.Select(b => b.ToString("X2"))));
    }
}
