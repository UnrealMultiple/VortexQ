using PuppeteerSharp;

namespace GitHook;

internal class GithubPageUtils
{
    private static IBrowser? browser;

    public static async Task<byte[]> ScreenPage(string url, string? click = null)
    {
        if (browser == null || !browser.IsConnected || browser.IsClosed || browser.Process.HasExited)
        {
            await new BrowserFetcher().DownloadAsync();
            browser = await Puppeteer.LaunchAsync(new LaunchOptions()
            {
                Headless = true,
            });
        }
        using var page = await browser.NewPageAsync();
        await page.GoToAsync(url, WaitUntilNavigation.Load).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(click))
            await page.ClickAsync(click);

        var ret = await page.ScreenshotDataAsync(new()
        {
            Type = ScreenshotType.Png,
            FullPage = true
        });
        await page.CloseAsync();
        return ret;
    }
}
