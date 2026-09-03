using System.Data.Common;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;

using Dapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Provides a cross-compatible implementation of identity user persistence using Dapper ADO.NET abstractions.
/// Integrates directly with ASP.NET Core Identity store contracts.
/// </summary>
public sealed partial class DapperUserRepository : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, IUserEmailStore<ApplicationUser>
{
    /// <summary>
    /// The database connection factory used to acquire asynchronous connections.
    /// </summary>
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// The database configuration options, including timeout settings.
    /// </summary>
    private readonly DatabaseOptions _databaseOptions;

    /// <summary>
    /// The structured logging instance for repository diagnostics.
    /// </summary>
    private readonly ILogger<DapperUserRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperUserRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The factory used to establish database connections.</param>
    /// <param name="databaseOptions">The options containing database configuration settings.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionFactory"/>, <paramref name="databaseOptions"/>, or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public DapperUserRepository(
        IDbConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<DapperUserRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = """
            INSERT INTO users (id, user_name, normalized_user_name, email, normalized_email, password_hash, display_name, created_at)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @PasswordHash, @DisplayName, @CreatedAt);
            """;

        LogCreatingUser(_logger, user.Email);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, user, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rows = await connection.ExecuteAsync(command).ConfigureAwait(false);

        return rows > 0
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Description = "Could not insert user into persistence store." });
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = """
            UPDATE users 
            SET user_name = @UserName,
                normalized_user_name = @NormalizedUserName,
                email = @Email,
                normalized_email = @NormalizedEmail,
                password_hash = @PasswordHash,
                display_name = @DisplayName,
                last_login_at = @LastLoginAt
            WHERE id = @Id;
            """;

        LogUpdatingUser(_logger, user.Id);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, user, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rows = await connection.ExecuteAsync(command).ConfigureAwait(false);

        return rows > 0
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Description = "Could not update user in persistence store." });
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = "DELETE FROM users WHERE id = @Id;";

        LogDeletingUser(_logger, user.Id);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, new { user.Id }, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rows = await connection.ExecuteAsync(command).ConfigureAwait(false);

        return rows > 0
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Description = "Could not delete user from persistence store." });
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 
                id AS Id,
                user_name AS UserName,
                normalized_user_name AS NormalizedUserName,
                email AS Email,
                normalized_email AS NormalizedEmail,
                password_hash AS PasswordHash,
                display_name AS DisplayName,
                created_at AS CreatedAt,
                last_login_at AS LastLoginAt
            FROM users 
            WHERE id = @Id;
            """;

        LogFindingUserById(_logger, userId);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, new { Id = userId }, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 
                id AS Id,
                user_name AS UserName,
                normalized_user_name AS NormalizedUserName,
                email AS Email,
                normalized_email AS NormalizedEmail,
                password_hash AS PasswordHash,
                display_name AS DisplayName,
                created_at AS CreatedAt,
                last_login_at AS LastLoginAt
            FROM users 
            WHERE normalized_user_name = @NormalizedUserName;
            """;

        LogFindingUserByName(_logger, normalizedUserName);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, new { NormalizedUserName = normalizedUserName }, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 
                id AS Id,
                user_name AS UserName,
                normalized_user_name AS NormalizedUserName,
                email AS Email,
                normalized_email AS NormalizedEmail,
                password_hash AS PasswordHash,
                display_name AS DisplayName,
                created_at AS CreatedAt,
                last_login_at AS LastLoginAt
            FROM users 
            WHERE normalized_email = @NormalizedEmail;
            """;

        LogFindingUserByEmail(_logger, normalizedEmail);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, new { NormalizedEmail = normalizedEmail }, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Id);
    }

    /// <inheritdoc />
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.UserName);
    }

    /// <inheritdoc />
    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.UserName = userName;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.NormalizedUserName);
    }

    /// <inheritdoc />
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.PasswordHash);
    }

    /// <inheritdoc />
    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    /// <inheritdoc />
    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.Email = email;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Email);
    }

    /// <inheritdoc />
    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.EmailConfirmed);
    }

    /// <inheritdoc />
    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.NormalizedEmail);
    }

    /// <inheritdoc />
    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Debug,
        Message = "Creating user record for email {Email}.")]
    private static partial void LogCreatingUser(ILogger logger, string? email);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Debug,
        Message = "Updating user record for user ID {UserId}.")]
    private static partial void LogUpdatingUser(ILogger logger, string userId);

    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Debug,
        Message = "Deleting user record for user ID {UserId}.")]
    private static partial void LogDeletingUser(ILogger logger, string userId);

    [LoggerMessage(
        EventId = 304,
        Level = LogLevel.Debug,
        Message = "Finding user by user ID {UserId}.")]
    private static partial void LogFindingUserById(ILogger logger, string userId);

    [LoggerMessage(
        EventId = 305,
        Level = LogLevel.Debug,
        Message = "Finding user by normalized username {NormalizedUserName}.")]
    private static partial void LogFindingUserByName(ILogger logger, string normalizedUserName);

    [LoggerMessage(
        EventId = 306,
        Level = LogLevel.Debug,
        Message = "Finding user by normalized email {NormalizedEmail}.")]
    private static partial void LogFindingUserByEmail(ILogger logger, string normalizedEmail);
}
