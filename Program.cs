using Avalonia;
using System.Threading;
namespace PrayerTimes;
static class Program {
    public static EventWaitHandle? ShowSignal;
    [STAThread]
    public static void Main(string[] args) {
        var mutex = new Mutex(true, "PrayerTimesTray_Single", out bool first);
        try { ShowSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "PrayerTimesTray_Show"); } catch { }
        if (!first) { ShowSignal?.Set(); return; }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
        GC.KeepAlive(mutex);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
