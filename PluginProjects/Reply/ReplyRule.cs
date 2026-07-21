using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lagrange.Core;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;

namespace Reply;

public class ReplyRule
{
    private Regex? _triggerRegex;

    [JsonPropertyName("MatchPattern")]
    public string MatchPattern { get; set; } = string.Empty;

    [JsonIgnore]
    public Regex TriggerRegex
    {
        get
        {
            if (_triggerRegex == null && !string.IsNullOrEmpty(MatchPattern))
                _triggerRegex = new Regex(MatchPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return _triggerRegex ?? new Regex("^$");
        }
        set => _triggerRegex = value;
    }

    [JsonPropertyName("ReplyTemplate")]
    public string ReplyTemplate { get; set; } = string.Empty;

    public ReplyRule() { }

    public ReplyRule(string matchPattern, string replyTemplate)
    {
        MatchPattern = matchPattern;
        ReplyTemplate = replyTemplate;
    }
}

public delegate Task<string> AsyncVariableHandler(string varName, string? param, MessageChain chain, IReadOnlyDictionary<string, string> context);
public delegate Task ContentTypeHandler(string type, string content, BotContext bot, BotMessage msg, MessageChain chain, MessageBuilder builder);

public partial class ReplyAdapter
{
    private static List<ReplyRule> Rules => Config.Instance.Rules;
    private static readonly Dictionary<string, AsyncVariableHandler> _asyncHandlers = [];
    private static readonly Dictionary<string, ContentTypeHandler> _contentHandlers = [];

    public static Action<string> Logger { get; set; } = Console.WriteLine;

    public static void RegisterAsyncHandler(string varName, AsyncVariableHandler handler)
    {
        _asyncHandlers[varName.ToLower()] = handler;
    }

    public static void RemoveAsyncHandler(string varName)
    {
        _asyncHandlers.Remove(varName.ToLower());
    }

    public static void RemoveContentHandler(string contentType)
    {
        _contentHandlers.Remove(contentType.ToLower());
    }

    public static void RegisterContentHandler(string contentType, ContentTypeHandler handler)
    {
        _contentHandlers[contentType.ToLower()] = handler;
    }

    public static List<string> GetVariables() => [.. _asyncHandlers.Keys];
    public static List<string> GetContentHandlers() => [.. _contentHandlers.Keys];

    public static async Task<MessageBuilder?> ProcessMessageAsync(MessageChain chain, BotContext bot, BotMessage msg, IReadOnlyDictionary<string, string>? context = null)
    {
        var message = GetText(chain).Trim();
        foreach (var rule in Rules)
        {
            var match = rule.TriggerRegex.Match(message);
            if (!match.Success) continue;
            var processed = await ProcessTemplateAsync(match, rule.ReplyTemplate, chain, context ?? new Dictionary<string, string>());
            return await BuildResponseAsync(processed, chain, bot, msg);
        }
        return null;
    }

    public static string GetText(MessageChain chain)
    {
        return string.Join("", chain.OfType<TextEntity>().Select(t => t.Text));
    }

    public static List<ImageEntity> GetImages(MessageChain chain)
    {
        return chain.OfType<ImageEntity>().ToList();
    }

    public static List<MentionEntity> GetMentions(MessageChain chain)
    {
        return chain.OfType<MentionEntity>().ToList();
    }

    private static async Task<string> ProcessTemplateAsync(Match match, string template, MessageChain chain, IReadOnlyDictionary<string, string> context)
    {
        var step1 = await ReplaceVariablesAsync(template, chain, context);
        return ReplaceRegexGroups(match, step1);
    }

    private static string ReplaceRegexGroups(Match match, string input)
    {
        return ReplaceGroup().Replace(input, m =>
        {
            if (!int.TryParse(m.Groups[1].Value, out int index)) return m.Value;

            if (index <= 0 || index >= match.Groups.Count)
            {
                return m.Value;
            }
            var value = match.Groups[index].Value;
            return value;
        });
    }

    private static async Task<string> ReplaceVariablesAsync(string input, MessageChain chain, IReadOnlyDictionary<string, string> context)
    {
        var pattern = @"\$(\w+)(?::([^$]+))?";
        var replacements = new Dictionary<string, string>();

        foreach (Match m in Regex.Matches(input, pattern))
        {
            var varName = m.Groups[1].Value.ToLower();
            var parameter = m.Groups[2].Success ? m.Groups[2].Value : null;

            // 优先从上下文取
            if (context.TryGetValue(varName, out var ctxValue))
            {
                replacements[m.Value] = ctxValue;
                continue;
            }

            if (_asyncHandlers.TryGetValue(varName, out var handler))
            {
                try
                {
                    var value = await handler(varName, parameter, chain, context);
                    replacements[m.Value] = value;
                }
                catch (Exception)
                {
                    replacements[m.Value] = $"[{varName}错误]";
                }
            }
        }

        return Regex.Replace(input, pattern, m =>
            replacements.GetValueOrDefault(m.Value, m.Value));
    }

    private static async Task<MessageBuilder> BuildResponseAsync(string processed, MessageChain chain, BotContext bot, BotMessage msg)
    {
        var pattern = @"{(?<type>\w+):\s*(?<content>[^}]+?)\s*}|(?<text>[^{]*)";
        var builder = new MessageBuilder();
        foreach (Match m in Regex.Matches(processed, pattern))
        {
            if (m.Groups["type"].Success)
            {
                var type = m.Groups["type"].Value.ToLower();
                var content = m.Groups["content"].Value.Trim();

                if (_contentHandlers.TryGetValue(type, out var handler))
                {
                    try
                    {
                        await handler(type, content, bot, msg, chain, builder);
                    }
                    catch (Exception)
                    {
                        Logger($"处理内容失败: {type}");
                    }
                }
            }
            else
            {
                var text = m.Groups["text"].Value.Trim();
                if (!string.IsNullOrEmpty(text))
                    builder.Text(text);
            }
        }

        return builder;
    }

    [GeneratedRegex(@"\$(\d+)")]
    public static partial Regex ReplaceGroup();
}
