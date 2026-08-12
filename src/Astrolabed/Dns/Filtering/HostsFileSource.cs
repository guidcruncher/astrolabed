using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Astrolabed.Dns.Filtering;

public sealed class HostsFileSource : IHostsFileSource
{
    private readonly IEnumerable<string> _paths;
    private readonly ILogger<HostsFileSource> _logger;

    public HostsFileSource(IEnumerable<string> paths, ILogger<HostsFileSource> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<IEnumerable<HostsEntry>> LoadAsync()
    {
        var list = new List<HostsEntry>();

        foreach (var path in _paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Hosts file not found: {Path}", fullPath);
                continue;
            }

            _logger.LogInformation("Loading hosts file: {Path}", path);

            string[] lines;

            try
            {
                lines = await File.ReadAllLinesAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read hosts file: {Path}", path);
                continue;
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var hashIndex = line.IndexOf('#');
                if (hashIndex >= 0)
                {
                    line = line[..hashIndex].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                }

                var parts = line.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    _logger.LogWarning("Invalid hosts entry (not enough tokens) in {Path}: {Line}", path, raw);
                    continue;
                }

                if (!IPAddress.TryParse(parts[0], out var ip))
                {
                    _logger.LogWarning("Invalid IP address in hosts file {Path}: {IP}", path, parts[0]);
                    continue;
                }

                for (int i = 1; i < parts.Length; i++)
                {
                    var domain = parts[i].Trim().ToLowerInvariant();

                    if (string.IsNullOrWhiteSpace(domain))
                    {
                        _logger.LogWarning("Empty hostname token in {Path}: {Line}", path, raw);
                        continue;
                    }

                    list.Add(new HostsEntry
                    {
                        Domain = domain,
                        Address = ip,
                        Source = path
                    });
                }
            }

            _logger.LogInformation("Finished loading hosts file {Path}: {Count} entries", path, list.Count);
        }

        return list;
    }
}
