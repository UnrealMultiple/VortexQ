namespace Music.Kugou;

public sealed record KugouQrCodeResult
{
    /// <summary>二维码原始 key，用于轮询登录状态</summary>
    public string QrCodeKey { get; init; } = "";

    /// <summary>完整的二维码扫描URL</summary>
    public string QrCodeUrl { get; init; } = "";

    /// <summary>二维码图片URL（通过在线服务生成）</summary>
    public string? QrCodeImageUrl { get; init; }

    /// <summary>二维码图片字节（来自API的base64 data URI解码）</summary>
    public byte[]? QrCodeImageBytes { get; init; }
}

public sealed record QrLoginResult
{
    public int Status { get; init; }
    public string? UserId { get; init; }
    public string? Nickname { get; init; }
    public string? Avatar { get; init; }
    public string? Token { get; init; }
}
