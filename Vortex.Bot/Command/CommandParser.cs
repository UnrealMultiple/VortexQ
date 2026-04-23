using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Vortex.Bot.Command;

internal static class CommandParser
{
    public delegate bool Parser(string arg, [MaybeNullWhen(false)] out object obj);

    private static bool TryParseBool(string arg, out object obj)
    {
        var result = bool.TryParse(arg, out var t);
        obj = t;
        return result;
    }

    private static bool TryParseUint(string arg, out object obj)
    {
        var result = uint.TryParse(arg, out var t);
        obj = t;
        return result;
    }

    private static bool TryParseInt(string arg, out object obj)
    {
        var result = int.TryParse(arg, out var t);
        obj = t;
        return result;
    }

    private static bool TryParseLong(string arg, out object obj)
    {
        var result = long.TryParse(arg, out var t);
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

    private static readonly Dictionary<Type, Parser> Parsers = new()
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

    public static Parser GetParser(Type type)
    {
        if (Parsers.TryGetValue(type, out var parser))
        {
            return parser;
        }
        throw new NotSupportedException($"Type {type.Name} is not supported as a command parameter");
    }

    public static string GetFriendlyName(Type type)
    {
        if (FriendlyNames.TryGetValue(type, out var name))
        {
            return name;
        }
        return type.Name.ToLower();
    }

    public static bool IsSupportedType(Type type)
    {
        return Parsers.ContainsKey(type);
    }
}
