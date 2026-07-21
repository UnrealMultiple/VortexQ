using System.Net;

namespace Reply;

public class FileReader
{
    public static byte[] ReadFileBuffer(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return uri.Scheme switch
            {
                "file" => HandleFileUri(uri),
                "http" => HandleWebUri(uri),
                "https" => HandleWebUri(uri),
                _ => throw new NotSupportedException($"Unsupported URI scheme: {uri.Scheme}"),
            };
        }
        else
        {
            return HandleLocalPath(path);
        }
    }

    private static byte[] HandleFileUri(Uri uri)
    {
        string localPath = uri.LocalPath;
        return File.ReadAllBytes(localPath);
    }

    private static byte[] HandleWebUri(Uri uri)
    {
        using var client = new HttpClient();
        return client.GetByteArrayAsync(uri).Result;
    }

    private static byte[] HandleLocalPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return File.ReadAllBytes(fullPath);
    }
}
