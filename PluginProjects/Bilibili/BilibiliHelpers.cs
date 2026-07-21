using System.Text.RegularExpressions;

internal static partial class BilibiliHelpers
{

    [GeneratedRegex(@"[^0-9A-Za-z]*AV(?<AID>[0-9]+).*")]
    public static partial Regex Av_Regex();


    [GeneratedRegex(@".*(?<B23>b23\.tv/[a-zA-Z0-9]+).*")]
    public static partial Regex B23_Regex();

    [GeneratedRegex(".*(?<BVID>BV[0-9A-Za-z]+).*")]
    public static partial Regex BVIDRegex();


    [GeneratedRegex(@"[^0-9A-Za-z]*(?<BVID>BV[0-9A-Za-z]+).*")]
    public static partial Regex Bv_Regex();
}