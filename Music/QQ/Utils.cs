using System.Web;

namespace Music.QQ;

public class Utils
{
    public static string QueryUri(string url, Dictionary<string, string>? @params = null)
    {
        var uri = new UriBuilder(url);
        var args = HttpUtility.ParseQueryString(uri.Query);
        if (@params is not null)
            foreach (var (key, value) in @params)
                args[key] = value;
        uri.Query = args.ToString();
        return uri.ToString();
    }

    public static long GetSearchID()
    {
        var random = new Random();
        int e = random.Next(1, 21);
        long t = e * 18014398509481984L;
        long n = random.Next(0, 4194305) * 4294967296L;
        long a = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        long r = a % (24L * 60 * 60 * 1000);

        return t + n + r;
    }

    public static int Hash33(string s, int h = 0)
    {
        unchecked
        {
            foreach (char c in s)
            {
                h = (h << 5) + h + c;
            }
            return 2147483647 & h;
        }
    }
}
