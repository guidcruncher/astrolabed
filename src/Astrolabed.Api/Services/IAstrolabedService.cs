namespace Astrolabed.Api.Services;

/// <summary>
/// Defines business operations for the Astrolabed service module.
/// </summary>
public interface IAstrolabedService
{
    /// <summary>
    /// Retrieves core system status data.
    /// </summary>
    /// <returns>A string containing system operational details.</returns>
    string GetSystemStatus();
}
