using System.Text;

namespace Vortex.Bot.Extension;

public static class IEnumerableExtension
{
    private static readonly Random _random = new Random();
    public static string ToJoinedString<T>(this IEnumerable<T> source, string separator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(separator);
        var sb = new StringBuilder();
        using var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
        {
            sb.Append(enumerator.Current);
            while (enumerator.MoveNext())
            {
                sb.Append(separator);
                sb.Append(enumerator.Current);
            }
        }
        return sb.ToString();
    }

    public static T Rand<T>(this IEnumerable<T> source)
    {
        return source.ElementAt(_random.Next(0, source.Count()));
    }

    public static string ToJoinedString<T>(this IEnumerable<T> source, Func<T, string> selector, string separator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(separator);
        var sb = new StringBuilder();
        using var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
        {
            sb.Append(selector(enumerator.Current));
            while (enumerator.MoveNext())
            {
                sb.Append(separator);
                sb.Append(selector(enumerator.Current));
            }
        }
        return sb.ToString();
    }
}