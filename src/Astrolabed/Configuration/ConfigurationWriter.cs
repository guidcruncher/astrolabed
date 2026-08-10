using System.IO;
using System.Text.Json;

using Astrolabed.Hosting;

namespace Astrolabed.Configuration;

public class ConfigurationWriter
{

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public boolean Write(ServerOptions options)
    {
        if (String.IsNullOrEmpty(HostBuilderFactory.ConfigurationFile))
        {
            return false;
        }

        var fullPath = Path.Combine(AppContext.BaseDirectory, HostBuilderFactory.ConfigurationFile);
        var backupFilePath = $"{fullPath}.bak";

        if (File.Exists(fullPath))
        {
            var backupFilePath = $"{fullPath}.bak";
            File.Copy(fullPath, backupFilePath, overwrite: true);
        }

        string json = JsonSerializer.Serialize(options, SerializerOptions);
        File.WriteAllText(fullPath, json);
        return true;
    }

}
