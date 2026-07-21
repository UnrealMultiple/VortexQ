using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Music.QQ.Internal;

public class QimeiService
{
    private const string PUBLIC_KEY = "-----BEGIN PUBLIC KEY-----\n" +
        "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDEIxgwoutfwoJxcGQeedgP7FG9qaIuS0qzfR8gWkrkTZKM2iWHn2ajQpBRZjMSoSf6+KJGvar2ORhBfpDXyVtZCKpqLQ+FLkpncClKVIrBwv6PHyUvuCb0rIarmgDnzkfQAqVufEtR64iazGDKatvJ9y6B9NMbHddGSAUmRTCrHQIDAQAB\n" +
        "-----END PUBLIC KEY-----";
    private const string SECRET = "ZdJqM15EeO2zWc08";
    private const string APP_KEY = "0AND0HD6FE4HY80F";

    public class QimeiResult
    {
        [JsonPropertyName("q16")]
        public string Q16 { get; set; } = string.Empty;
        [JsonPropertyName("q36")]
        public string Q36 { get; set; } = string.Empty;
    }

    public static string CalcMd5(params object[] strings)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(string.Join("", strings));
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public static byte[] RsaEncrypt(byte[] content)
    {
        using (var rsa = new RSACryptoServiceProvider())
        {
            rsa.ImportFromPem(PUBLIC_KEY);
            return rsa.Encrypt(content, false);
        }
    }

    public static byte[] AesEncrypt(byte[] key, byte[] content)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = key;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            byte[] paddedContent = PadContent(content, 16);
            return encryptor.TransformFinalBlock(paddedContent, 0, paddedContent.Length);
        }
    }

    private static byte[] PadContent(byte[] content, int blockSize)
    {
        int paddingSize = blockSize - (content.Length % blockSize);
        byte[] paddedContent = new byte[content.Length + paddingSize];
        Buffer.BlockCopy(content, 0, paddedContent, 0, content.Length);
        for (int i = 0; i < paddingSize; i++)
        {
            paddedContent[content.Length + i] = (byte)paddingSize;
        }
        return paddedContent;
    }

    public static string RandomBeaconId()
    {
        var beaconId = new StringBuilder();
        string timeMonth = DateTime.Now.ToString("yyyy-MM-") + "01";
        Random rand = new Random();
        for (int i = 1; i <= 40; i++)
        {
            if (new[] { 1, 2, 13, 14, 17, 18, 21, 22, 25, 26, 29, 30, 33, 34, 37, 38 }.Contains(i))
            {
                beaconId.Append($"k{i}:{timeMonth}{rand.Next(100000, 999999)}.{rand.Next(100000000, 999999999)}");
            }
            else if (i == 3)
            {
                beaconId.Append("k3:0000000000000000");
            }
            else if (i == 4)
            {
                beaconId.Append($"k4:{new string(Enumerable.Repeat("123456789abcdef", 16 / 15 + 1).Select(s => s[Random.Shared.Next(s.Length)]).Take(16).ToArray())}");
            }
            else
            {
                beaconId.Append($"k{i}:{rand.Next(0, 9999)}");
            }
            beaconId.Append(";");
        }
        return beaconId.ToString();
    }

    public static Dictionary<string, object> RandomPayloadByDevice(Device device, string version)
    {
        Random rand = new Random();
        int fixedRand = rand.Next(0, 14400);
        var reserved = new Dictionary<string, object>
        {
            ["harmony"] = "0",
            ["clone"] = "0",
            ["containe"] = "",
            ["oz"] = "UhYmelwouA+V2nPWbOvLTgN2/m8jwGB+yUB5v9tysQg=",
            ["oo"] = "Xecjt+9S1+f8Pz2VLSxgpw==",
            ["kelong"] = "0",
            ["uptimes"] = (DateTime.Now - TimeSpan.FromSeconds(fixedRand)).ToString("yyyy-MM-dd HH:mm:ss"),
            ["multiUser"] = "0",
            ["bod"] = device.Brand,
            ["dv"] = device.DeviceModel,
            ["firstLevel"] = "",
            ["manufact"] = device.Brand,
            ["name"] = device.Model,
            ["host"] = "se.infra",
            ["kernel"] = device.ProcVersion,
        };

        return new Dictionary<string, object>
        {
            ["androidId"] = device.AndroidId,
            ["platformId"] = 1,
            ["appKey"] = APP_KEY,
            ["appVersion"] = version,
            ["beaconIdSrc"] = RandomBeaconId(),
            ["brand"] = device.Brand,
            ["channelId"] = "10003505",
            ["cid"] = "",
            ["imei"] = device.Imei,
            ["imsi"] = "",
            ["mac"] = "",
            ["model"] = device.Model,
            ["networkType"] = "unknown",
            ["oaid"] = "",
            ["osVersion"] = $"Android {device.Version.Release},level {device.Version.Sdk}",
            ["qimei"] = "",
            ["qimei36"] = "",
            ["sdkVersion"] = "1.2.13.6",
            ["targetSdkVersion"] = "33",
            ["audit"] = "",
            ["userId"] = "{}",
            ["packageId"] = "com.tencent.qqmusic",
            ["deviceType"] = "Phone",
            ["sdkName"] = "",
            ["reserved"] = reserved
        };
    }

    public static async Task<QimeiResult> GetQimeiAsync(Device device, string version)
    {
        try
        {
            var payload = RandomPayloadByDevice(device, version);
            string cryptKey = new string(Enumerable.Repeat("adbcdef1234567890", 16 / 15 + 1).Select(s => s[Random.Shared.Next(s.Length)]).Take(16).ToArray());
            string nonce = new string(Enumerable.Repeat("adbcdef1234567890", 16 / 15 + 1).Select(s => s[Random.Shared.Next(s.Length)]).Take(16).ToArray());
            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string key = Convert.ToBase64String(RsaEncrypt(Encoding.UTF8.GetBytes(cryptKey)));
            string paramsValue = Convert.ToBase64String(AesEncrypt(Encoding.UTF8.GetBytes(cryptKey), Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));
            string extra = $"{{\"appKey\":\"{APP_KEY}\"}}";
            string sign = CalcMd5(key, paramsValue, $"{ts * 1000}", nonce, SECRET, extra);

            using var client = new HttpClient();
            var requestJson = new JsonObject
            {
                ["app"] = 0,
                ["os"] = 1,
                ["qimeiParams"] = new JsonObject
                {
                    ["key"] = key,
                    ["params"] = paramsValue,
                    ["time"] = ts.ToString(),
                    ["nonce"] = nonce,
                    ["sign"] = sign,
                    ["extra"] = extra
                }
            };

            var content = new StringContent(requestJson.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.tencentmusic.com/tme/trpc/proxy", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var data = JsonNode.Parse(responseString)?["data"];

            return new QimeiResult
            {
                Q16 = data?["q16"]?.ToString() ?? "",
                Q36 = data?["q36"]?.ToString() ?? "6c9d3cd110abca9b16311cee10001e717614"
            };
        }
        catch (Exception)
        {
            return new QimeiResult { Q16 = "", Q36 = "6c9d3cd110abca9b16311cee10001e717614" };
        }
    }
}
