using System.Reflection;
using System.Text;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal sealed class ArgumentBinder
{
    private readonly ArgumentParser[] _parsers;
    private readonly Type[] _types;
    private readonly string _parameterInfo;

    public ArgumentBinder(ParameterInfo[] parameters)
    {
        var commandParameters = parameters.Skip(1).ToArray();
        _parsers = CreateParsers(commandParameters);
        _types = CreateTypes(commandParameters);
        _parameterInfo = BuildParameterInfo(commandParameters);
    }

    public string ParameterInfo => _parameterInfo;

    public object?[]? ParseArguments(List<string> parameters, int startIndex)
    {
        var count = _parsers.Length;
        var args = new object?[count + 1];
        args[0] = null;

        for (var i = 0; i < count; i++)
        {
            var paramIndex = startIndex + i;

            if (paramIndex >= parameters.Count)
            {
                args[i + 1] = GetDefaultValue(_types[i]);
                continue;
            }

            if (!_parsers[i](parameters[paramIndex], out args[i + 1]))
                return null;
        }

        return args;
    }

    public ValidationResult GetValidationRules(int startIndex, ExecutorType executorType, int minArgs)
    {
        if (executorType == ExecutorType.Flexible)
        {
            return new ValidationResult(minArgs + startIndex, int.MaxValue);
        }

        var expectedCount = _parsers.Length + startIndex;
        return new ValidationResult(expectedCount, expectedCount);
    }

    private static ArgumentParser[] CreateParsers(ParameterInfo[] parameters) => [.. parameters.Select(p =>
        {
            return !CommandParser.IsSupportedType(p.ParameterType)
                ? throw new NotSupportedException(
                    $"Parameter type {p.ParameterType.Name} is not supported")
                : CommandParser.GetParser(p.ParameterType); })];

    private static Type[] CreateTypes(ParameterInfo[] parameters) => [.. parameters.Select(p => p.ParameterType)];

    private static string BuildParameterInfo(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0)
            return "";

        var sb = new StringBuilder();
        foreach (ParameterInfo p in parameters)
        {
            var paramAttr = p.GetCustomAttribute<ParamAttribute>();
            var paramDesc = paramAttr?.Description ?? p.Name ?? "param";
            sb.Append($" <{paramDesc}: {CommandParser.GetFriendlyName(p.ParameterType)}>");
        }
        return sb.ToString();
    }

    private static object? GetDefaultValue(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null)
            return null;

        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(int))
            return 0;
        if (type == typeof(long))
            return 0L;
        if (type == typeof(double))
            return 0.0;
        return type == typeof(bool) ? false : type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
