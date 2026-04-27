using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Vortex.Bot.Command;

internal delegate bool ArgumentParser(string arg, [MaybeNullWhen(false)] out object obj);

internal static class CommandParser
{
    private static readonly Dictionary<Type, ArgumentParser> Parsers = new()
    {
        [typeof(bool)] = TryParseBool,
        [typeof(uint)] = TryParseUint,
        [typeof(int)] = TryParseInt,
        [typeof(long)] = TryParseLong,
        [typeof(ulong)] = TryParseUlong,
        [typeof(string)] = TryParseString,
        [typeof(DateTime)] = TryParseDateTime,
        [typeof(double)] = TryParseDouble,
        [typeof(float)] = TryParseFloat,
    };

    private static readonly Dictionary<Type, string> FriendlyNames = new()
    {
        [typeof(bool)] = "bool",
        [typeof(uint)] = "uint",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(string)] = "str",
        [typeof(DateTime)] = "date",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
    };

    public static ArgumentParser GetParser(Type type)
    {
        if (Parsers.TryGetValue(type, out ArgumentParser? parser))
            return parser;

        if (Nullable.GetUnderlyingType(type) is Type underlyingType &&
            Parsers.TryGetValue(underlyingType, out parser))
        {
#pragma warning disable CS8622
            return (arg, out obj) =>
            {
                var result = parser(arg, out var parsedObj);
#pragma warning disable CS8601
                obj = parsedObj;
#pragma warning restore CS8601
                return result;
            };
#pragma warning restore CS8622
        }

        throw new NotSupportedException($"Type {type.Name} is not supported as a command parameter");
    }

    public static string GetFriendlyName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            var baseName = FriendlyNames.TryGetValue(underlyingType, out var name)
                ? name
                : underlyingType.Name.ToLower();
            return baseName + "?";
        }

        return FriendlyNames.TryGetValue(type, out var friendlyName)
            ? friendlyName
            : type.Name.ToLower();
    }

    public static bool IsSupportedType(Type type)
    {
        if (Parsers.ContainsKey(type))
            return true;

        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            return Parsers.ContainsKey(underlyingType);

        return false;
    }

    public static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static bool TryParseBool(string arg, out object obj)
    {
        bool result = bool.TryParse(arg, out bool t);
        obj = t;
        return result;
    }

    private static bool TryParseUint(string arg, out object obj)
    {
        bool result = uint.TryParse(arg, out uint t);
        obj = t;
        return result;
    }

    private static bool TryParseInt(string arg, out object obj)
    {
        bool result = int.TryParse(arg, out int t);
        obj = t;
        return result;
    }

    private static bool TryParseLong(string arg, out object obj)
    {
        bool result = long.TryParse(arg, out long t);
        obj = t;
        return result;
    }

    private static bool TryParseString(string arg, out object obj)
    {
        obj = arg;
        return true;
    }

    private static bool TryParseDateTime(string arg, out object obj)
    {
        var result = DateTime.TryParse(arg, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t);
        obj = t;
        return result;
    }

    private static bool TryParseDouble(string arg, out object obj)
    {
        var result = double.TryParse(arg, CultureInfo.InvariantCulture, out var t);
        obj = t;
        return result;
    }

    private static bool TryParseFloat(string arg, out object obj)
    {
        var result = float.TryParse(arg, CultureInfo.InvariantCulture, out var t);
        obj = t;
        return result;
    }

    private static bool TryParseUlong(string arg, out object obj)
    {
        var result = ulong.TryParse(arg, out var t);
        obj = t;
        return result;
    }
}
