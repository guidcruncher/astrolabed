using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabed;

public class LoggingOptions
{

    public const string SectionName = "Logging";

    public string Level { get; set; } = "Debug";

}
