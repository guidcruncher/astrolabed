using System.Data;
using System.Globalization;

using Dapper;

namespace Astrolabed.Data.Handlers;

/// <summary>
/// Custom Dapper type handler responsible for mapping between database string representations 
/// of ISO-8601 timestamps and .NET <see cref="DateTimeOffset"/> structures.
/// </summary>
public sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    /// <summary>
    /// Configures the specified database parameter with an ISO-8601 formatted string 
    /// representation of the provided <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value to convert and assign.</param>
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value.ToString("o", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses database column values into a .NET <see cref="DateTimeOffset"/> structure.
    /// </summary>
    /// <param name="value">The raw database value returned from the reader.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representation of the database value.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when <paramref name="value"/> cannot be converted to a <see cref="DateTimeOffset"/>.
    /// </exception>
    public override DateTimeOffset Parse(object value)
    {
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            string str when DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) => result,
            string str => DateTimeOffset.Parse(str, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().FullName} to {nameof(DateTimeOffset)}.")
        };
    }
}
