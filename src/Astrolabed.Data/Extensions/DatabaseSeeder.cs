using Astrolabed.Data.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Provides seeding functionality to populate the database with initial user data at application startup.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds an initial administrator user if no users currently exist in the store.
    /// </summary>
    /// <param name="serviceProvider">The root service provider used to create a service scope.</param>
    /// <param name="configuration">The configuration containing default seed credentials.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static async Task SeedInitialUserAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(configuration);

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        // Read seed credentials from appsettings / environment variables
        var seedEmail = "admin@astrolabed.local";
        var seedPassword = "Password123!";
        var seedDisplayName = "System Administrator";

        var existingUser = await userManager.FindByEmailAsync(seedEmail);
        if (existingUser is not null)
        {
            logger.LogInformation("Initial seed user '{Email}' already exists. Skipping seed.", seedEmail);
            return;
        }

        var initialUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = seedEmail,
            Email = seedEmail,
            DisplayName = seedDisplayName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(initialUser, seedPassword);
        if (result.Succeeded)
        {
            logger.LogInformation("Successfully seeded initial user '{Email}'.", seedEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed initial user '{Email}'. Errors: {Errors}", seedEmail, errors);
        }
    }
}
