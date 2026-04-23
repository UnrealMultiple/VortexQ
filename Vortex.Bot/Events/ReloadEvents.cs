using System.Diagnostics;

namespace Vortex.Bot.Events;

public static class ReloadEvents
{
    public static event Func<ReloadEventArgs, Task>? OnReload;

    public static long LastElapsedMilliseconds { get; private set; }

    public static async Task TriggerReloadAsync(long triggerUin)
    {
        var args = new ReloadEventArgs(triggerUin);
        var sw = Stopwatch.StartNew();

        if (OnReload != null)
        {
            await OnReload(args);
        }

        sw.Stop();
        args.ElapsedMilliseconds = sw.ElapsedMilliseconds;
        LastElapsedMilliseconds = sw.ElapsedMilliseconds;
    }
}

public class ReloadEventArgs(long triggerUin)
{
    public long TriggerUin { get; } = triggerUin;
    public long ElapsedMilliseconds { get; set; }
}
