using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Music.QQ.Internal;
public class Device
{
    private static readonly Random random = new();

    [JsonPropertyName("display")]
    public string Display { get; set; } = $"QMAPI.{random.Next(100000, 999999)}.001";

    [JsonPropertyName("product")]
    public string Product { get; set; } = "iarim";

    [JsonPropertyName("device")]
    public string DeviceModel { get; set; } = "sagit";

    [JsonPropertyName("board")]
    public string Board { get; set; } = "eomam";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "MI 6";

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } =
        $"xiaomi/iarim/sagit:10/eomam.200122.001/{random.Next(1000000, 9999999)}:user/release-keys";

    [JsonPropertyName("boot_id")]
    public string BootId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("proc_version")]
    public string ProcVersion { get; set; } =
        $"Linux 5.4.0-54-generic-{GenerateRandomString(8)} (android-build@google.com)";

    [JsonPropertyName("imei")]
    public string Imei { get; set; } = GenerateRandomImei();

    [JsonPropertyName("brand")]
    public string Brand { get; set; } = "Xiaomi";

    [JsonPropertyName("bootloader")]
    public string Bootloader { get; set; } = "U-boot";

    [JsonPropertyName("base_band")]
    public string BaseBand { get; set; } = "";

    [JsonPropertyName("version")]
    public OSVersion Version { get; set; } = new OSVersion();

    [JsonPropertyName("sim_info")]
    public string SimInfo { get; set; } = "T-Mobile";

    [JsonPropertyName("os_type")]
    public string OsType { get; set; } = "android";

    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = "00:50:56:C0:00:08";

    [JsonPropertyName("ip_address")]
    public List<int> IpAddress { get; } = [10, 0, 1, 3];

    [JsonPropertyName("wifi_bssid")]
    public string WifiBssid { get; set; } = "00:50:56:C0:00:08";

    [JsonPropertyName("wifi_ssid")]
    public string WifiSsid { get; set; } = "<unknown ssid>";

    [JsonPropertyName("imsi_md5")]
    public List<byte> ImsiMd5 { get; set; } = GenerateRandomMd5().ToList();

    [JsonPropertyName("android_id")]
    public string AndroidId { get; set; } = BitConverter.ToString(GenerateRandomBytes(8)).Replace("-", "").ToLower();

    [JsonPropertyName("apn")]
    public string Apn { get; set; } = "wifi";

    [JsonPropertyName("vendor_name")]
    public string VendorName { get; set; } = "MIUI";

    [JsonPropertyName("vendor_os_name")]
    public string VendorOsName { get; set; } = "qmapi";

    [JsonPropertyName("qimei")]
    public QimeiService.QimeiResult Qimei { get; set; } = new QimeiService.QimeiResult();


    private static string GenerateRandomString(int length) =>
        new string("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".OrderBy(s => Guid.NewGuid()).Take(length).ToArray());

    private static byte[] GenerateRandomBytes(int count)
    {
        var bytes = new byte[count];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    private static byte[] GenerateRandomMd5()
    {
        return MD5.HashData(GenerateRandomBytes(16));
    }

    public static string GenerateRandomImei()
    {
        int[] imei = new int[14];
        int sum = 0;

        for (int i = 0; i < 14; i++)
        {
            imei[i] = random.Next(0, 10);
            if ((i + 2) % 2 == 0)
            {
                int doubled = imei[i] * 2;
                imei[i] = doubled >= 10 ? doubled - 9 : doubled;
            }
            sum += imei[i];
        }

        int checkDigit = (10 - sum % 10) % 10;
        return string.Concat(imei) + checkDigit;
    }
}




public class OSVersion
{
    private static readonly Random random = new Random();

    [JsonPropertyName("incremental")]
    public string Incremental { get; set; } = $"QMAPI{random.Next(100000, 999999)}.001";

    [JsonPropertyName("codename")]
    public string Codename { get; set; } = "REL";

    [JsonPropertyName("release")]
    public string Release { get; set; } = "10";

    [JsonPropertyName("sdk")]
    public int Sdk { get; set; } = 29;
}
