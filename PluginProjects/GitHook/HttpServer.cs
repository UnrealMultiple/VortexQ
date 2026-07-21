using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Vortex.Bot;

namespace GitHook;

public class HttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly WebHook _webHook;

    public event Action<string, LogLevel>? Logger;

    public HttpServer(VortexContext vortex)
    {
        _listener = new HttpListener();
        _webHook = new WebHook(vortex);
    }

    public async Task Start()
    {
        if (!Config.Instance.Enable)
        {
            Logger?.Invoke("HTTP server is disabled in config", LogLevel.Warning);
            return;
        }
        _listener.Prefixes.Add($"http://*:{Config.Instance.Port}{Config.Instance.Path}");
        _listener.Start();
        Logger?.Invoke($"HTTP server started at http://*:{Config.Instance.Port}{Config.Instance.Path}", LogLevel.Information);
        try
        {
            while (true)
            {
                var context = await _listener.GetContextAsync();
                await ReceiveLoopAsync(context);
            }
        }
        catch (HttpListenerException ex)
        {
            Logger?.Invoke($"HTTP server error: {ex.Message}", LogLevel.Error);
            return;
        }
    }

    public void Stop()
    {
        _listener.Stop();
        Logger?.Invoke("HTTP server stopped", LogLevel.Information);
    }

    private async Task ReceiveLoopAsync(HttpListenerContext context)
    {
        try
        {
            using var sr = new StreamReader(context.Request.InputStream);
            var body = await sr.ReadToEndAsync();
            var header = context.Request.Headers;
            var dict = new Dictionary<string, StringValues>();
            foreach (var key in header.AllKeys)
            {
                if (key is null)
                    continue;
                dict[key] = new StringValues(header[key]);
            }
            await _webHook.ProcessWebhookAsync(dict, body);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Logger?.Invoke($"Error processing request: {ex.Message}", LogLevel.Error);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(Encoding.UTF8.GetBytes("OK"));
            context.Response.Close();
        }
    }

    public void Dispose()
    {
        _listener.Close();
        GC.SuppressFinalize(this);
    }
}
