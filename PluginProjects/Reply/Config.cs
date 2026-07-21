using System.Text.Json.Serialization;
using Vortex.Bot.Configuration;

namespace Reply;

public class Config : JsonConfigBase<Config>
{
    public override string FileName => "Reply";

    [JsonPropertyName("匹配列表")]
    public List<ReplyRule> Rules { get; set; } = [];

    public void RemoveRule(int index)
    {
        if (index < 1 || index > Rules.Count) return;
        Rules.RemoveAt(index - 1);
    }
}
