using System.ComponentModel.DataAnnotations;

namespace Astrolabed.Api.Models;

public sealed record LoginRequest(
    [Required] string Username,
    [Required] string Password);
