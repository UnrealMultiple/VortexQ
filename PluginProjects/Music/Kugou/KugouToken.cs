namespace Music.Kugou;

public sealed record KugouToken
{
    public string UserId { get; init; } = "0";
    public string Token { get; init; } = "";
    public string VipType { get; init; } = "0";
    public string VipToken { get; init; } = "";
    public string T1 { get; init; } = "";
    public string InstallGuid { get; init; } = "";
    public string InstallMac { get; init; } = "";
    public string InstallDev { get; init; } = "";
    public string Dfid { get; init; } = "";
    public string Nickname { get; init; } = "";
    public Dictionary<string, string> Cookies { get; init; } = new();
}
