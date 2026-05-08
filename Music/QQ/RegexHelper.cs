using System.Text.RegularExpressions;

namespace Music.QQ;

internal static partial class RegexHelper
{
    [GeneratedRegex("(?<=code=)(.+?)(?=&)")]
    public static partial Regex RegexCode();

    [GeneratedRegex("&uin=(.+?)&service")]
    public static partial Regex RegexUin();

    [GeneratedRegex("&ptsigx=(.+?)&s_url")]
    public static partial Regex RegexSigx();

    [GeneratedRegex("ptuiCB\\((.*?)\\)")]
    public static partial Regex RegexState();
}