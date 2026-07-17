using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Vortex.Bot.Configuration;

namespace TerrariaBridge.Config;

public class PrizeConfiguration : JsonConfigBase<PrizeConfiguration>
{
    [JsonIgnore]
    public override string FileName => "Prize";

    [JsonPropertyName("抽奖费用")]
    public long DrawCost { get; set; }

    [JsonPropertyName("奖池内容")]
    public List<PrizeItem> Prizes { get; set; } = [];

    public override void SetDefault()
    {
        DrawCost = 0;
        Prizes = [new PrizeItem()];
    }

    public PrizeItem PickPrize()
    {
        var totalWeight = Prizes.Sum(static prize => Math.Max(prize.Weight, 0));
        var target = Random.Shared.Next(1, totalWeight + 1);
        foreach (var prize in Prizes)
        {
            target -= Math.Max(prize.Weight, 0);
            if (target <= 0) return prize;
        }
        return Prizes[^1];
    }
}

public class PrizeItem
{
    [JsonPropertyName("奖品名称")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("奖品ID")]
    public int ItemId { get; set; }
    [JsonPropertyName("中奖概率")]
    public int Weight { get; set; }
    [JsonPropertyName("最大数量")]
    public int MaximumQuantity { get; set; }
    [JsonPropertyName("最小数量")]
    public int MinimumQuantity { get; set; }
}
