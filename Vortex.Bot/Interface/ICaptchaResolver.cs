namespace Vortex.Bot.Interface;

public interface ICaptchaResolver
{
    Task<(string, string)> ResolveCaptchaAsync(string url, CancellationToken token = default);
}
