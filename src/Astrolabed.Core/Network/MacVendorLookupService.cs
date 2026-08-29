using System.Collections.Frozen;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Core.Network;

/// <summary>
/// Provides high-performance, O(1) in-memory lookups of MAC address vendors using pre-loaded IEEE OUI records.
/// </summary>
public sealed class MacVendorLookupService : IMacVendorLookupService
{
    private const string CsvFileName = "mac-vendors-export.csv";
    private readonly FrozenDictionary<uint, MacVendorInfo> _lookupTable;
    private readonly ILogger<MacVendorLookupService> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="MacVendorLookupService"/> class and loads vendor data from the root CSV file.
    /// </summary>
    /// <param name="environment">The host environment providing access to the application content root path.</param>
    /// <param name="logger">The logger instance used for diagnostics and operational logging.</param>
    public MacVendorLookupService(IHostEnvironment environment, ILogger<MacVendorLookupService> logger)
    {
        _environment = environment;
        _logger = logger;
        string fullPath = Path.Combine(_environment.ContentRootPath, CsvFileName);
        _lookupTable = LoadCsvData(fullPath);
    }

    /// <inheritdoc />
    public MacVendorInfo? FindVendor(string macAddress)
    {
        if (macAddress is null)
        {
            return null;
        }

        if (TryExtractOuiKey(macAddress.AsSpan(), out uint ouiKey) && _lookupTable.TryGetValue(ouiKey, out var vendorInfo))
        {
            return vendorInfo;
        }

        return null;
    }

    /// <inheritdoc />
    public bool TryGetVendor(string macAddress, out MacVendorInfo? vendor)
    {
        vendor = FindVendor(macAddress);
        return vendor is not null;
    }

    private FrozenDictionary<uint, MacVendorInfo> LoadCsvData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("MAC vendor CSV file was not found at expected path: {FilePath}", filePath);
            return FrozenDictionary<uint, MacVendorInfo>.Empty;
        }

        var dictionary = new Dictionary<uint, MacVendorInfo>();

        try
        {
            using var reader = new StreamReader(filePath);

            // Skip header line
            string? header = reader.ReadLine();

            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (TryParseCsvLine(line, out uint ouiKey, out var vendorInfo))
                {
                    dictionary.TryAdd(ouiKey, vendorInfo);
                }
            }

            _logger.LogInformation("Successfully loaded {Count} MAC vendor OUI records into memory from {FilePath}.", dictionary.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MAC vendor CSV file from path: {FilePath}", filePath);
        }

        return dictionary.ToFrozenDictionary();
    }

    private static bool TryParseCsvLine(string line, out uint ouiKey, out MacVendorInfo vendorInfo)
    {
        ouiKey = 0;
        vendorInfo = null!;

        var fields = ParseCsvFields(line);
        if (fields.Count < 2)
        {
            return false;
        }

        string rawOui = fields[0].Trim();
        string vendorName = fields[1].Trim();
        bool isPrivate = fields.Count > 2 && bool.TryParse(fields[2].Trim(), out bool parsedPrivate) && parsedPrivate;
        string? blockType = fields.Count > 3 && !string.IsNullOrWhiteSpace(fields[3]) ? fields[3].Trim() : null;
        string? lastUpdate = fields.Count > 4 && !string.IsNullOrWhiteSpace(fields[4]) ? fields[4].Trim() : null;

        if (!TryExtractOuiKey(rawOui.AsSpan(), out ouiKey))
        {
            return false;
        }

        vendorInfo = new MacVendorInfo(rawOui, vendorName, isPrivate, blockType, lastUpdate);
        return true;
    }

    private static bool TryExtractOuiKey(ReadOnlySpan<char> input, out uint ouiKey)
    {
        ouiKey = 0;
        int hexCount = 0;
        uint value = 0;

        for (int i = 0; i < input.Length && hexCount < 6; i++)
        {
            char c = input[i];
            int hexDigit = GetHexValue(c);
            if (hexDigit != -1)
            {
                value = (value << 4) | (uint)hexDigit;
                hexCount++;
            }
        }

        if (hexCount == 6)
        {
            ouiKey = value;
            return true;
        }

        return false;
    }

    private static int GetHexValue(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => c - 'A' + 10,
            >= 'a' and <= 'f' => c - 'a' + 10,
            _ => -1
        };
    }

    private static List<string> ParseCsvFields(string line)
    {
        var fields = new List<string>(5);
        bool inQuotes = false;
        int startPosition = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(CleanQuotes(line.AsSpan(startPosition, i - startPosition)));
                startPosition = i + 1;
            }
        }

        if (startPosition <= line.Length)
        {
            fields.Add(CleanQuotes(line.AsSpan(startPosition)));
        }

        return fields;
    }

    private static string CleanQuotes(ReadOnlySpan<char> span)
    {
        span = span.Trim();
        if (span.Length >= 2 && span[0] == '\"' && span[^1] == '\"')
        {
            span = span[1..^1];
        }
        return span.ToString().Replace("\"\"", "\"");
    }
}
