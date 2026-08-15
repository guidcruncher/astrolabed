namespace Astrolabed.Api.Models;

public sealed record UserDto(
    string Username,
    bool IsAuthenticated);
