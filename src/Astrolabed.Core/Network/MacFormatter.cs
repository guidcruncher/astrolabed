namespace Astrolabed.Core.Network;

public static class MacAddressFormatter
{
    public static string Format(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return string.Empty;
        }

        Span<char> hex = stackalloc char[12];
        int count = 0;

        foreach (char c in macAddress)
        {
            if (char.IsAsciiHexDigit(c))
            {
                if (count < 12)
                {
                    hex[count++] = char.ToUpperInvariant(c);
                }
                else
                {
                    count++;
                    break;
                }
            }
        }

        if (count != 12)
        {
            return macAddress.Trim().ToUpperInvariant();
        }

        return $"{hex[0]}{hex[1]}:{hex[2]}{hex[3]}:{hex[4]}{hex[5]}:{hex[6]}{hex[7]}:{hex[8]}{hex[9]}:{hex[10]}{hex[11]}";
    }
}
