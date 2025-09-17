using System.Globalization;

namespace Chainly.Infrastructure.Helpers;

public static class DateTimeExtensions
{
    private const string DefaultFormat = "dd MMM. yyyy - hh:mmtt";

    public static string ToAppFormat(this DateTime date) =>
        date.ToString(DefaultFormat, CultureInfo.InvariantCulture);
}