using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using WinRT;

namespace FluentPrayerTimes;

public partial class App : Application
{
	private static Mutex? _mutex;

	public static string? Preview;

	public static string? PreviewPart;

	public static string? PreviewLang;

	public static string? E2EDetect;

	public static string? E2EClock;

	public static string? E2ELog;

	public static string? E2ESet;

	private MainWindow? _window;

	public static string AppData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FluentPrayerTimes");

	public App()
	{
		InitializeComponent();
        base.UnhandledException += (_, error) => Log.Error("ui-unhandled", error.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, error) => Log.Write("unhandled", (error.ExceptionObject as Exception)?.GetType().Name ?? "Fatal error");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, error) => { Log.Error("unobserved-task", error.Exception); error.SetObserved(); };
    }

    internal static bool TestMode => Environment.GetEnvironmentVariable("FPT_E2E") == "1";
    internal static void ReleaseInstance()
    {
        try { _mutex?.ReleaseMutex(); _mutex?.Dispose(); _mutex = null; }
        catch (Exception e) { Log.Error("instance-release", e); }
    }

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		foreach (string text in TestMode ? commandLineArgs : Array.Empty<string>())
		{
			if (text.StartsWith("--preview="))
			{
				Preview = text.Substring(10);
			}
			if (text.StartsWith("--part="))
			{
				PreviewPart = text.Substring(7);
			}
			if (text.StartsWith("--lang="))
			{
				PreviewLang = text.Substring(7);
			}
			if (text.StartsWith("--e2e-detect="))
			{
				E2EDetect = text.Substring(13);
			}
			if (text.StartsWith("--e2e-clock="))
			{
				E2EClock = text.Substring(12);
			}
			if (text.StartsWith("--e2e-log="))
			{
				string candidate = Path.GetFullPath(text.Substring(10));
                string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (candidate.StartsWith(temp, StringComparison.OrdinalIgnoreCase)) E2ELog = candidate;
			}
			if (text.StartsWith("--e2e-set="))
			{
				E2ESet = text.Substring(10);
			}
		}
		if (TestMode && commandLineArgs.Contains("--selftest"))
		{
			SelfTest.Run();
			return;
		}
		if (TestMode && commandLineArgs.Contains("--update-e2e"))
		{
			SelfTest.RunUpdateE2E();
			return;
		}
		L.Set(PreviewLang ?? Settings.Load().Lang);
		_mutex = new Mutex(initiallyOwned: true, "FluentPrayerTimes.SingleInstance", out var createdNew);
        if (!createdNew && commandLineArgs.Contains("--restarted"))
        {
            try { createdNew = _mutex.WaitOne(TimeSpan.FromSeconds(15)); }
            catch (AbandonedMutexException) { createdNew = true; }
        }
        if (!createdNew)
        {
            try { using var show = EventWaitHandle.OpenExisting("FluentPrayerTimes.Show"); show.Set(); }
            catch (Exception e) { Log.Error("instance-show", e); }
            Exit();
            return;
        }

		bool startHidden = Environment.GetCommandLineArgs().Any((string a) => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)) || Preview != null;
		_window = new MainWindow(startHidden);
		StartQuitListener(_window);
        StartShowListener(_window);
	}

    private static void StartShowListener(MainWindow window)
    {
        var thread = new Thread(() =>
        {
            try
            {
                using var show = new EventWaitHandle(false, EventResetMode.AutoReset, "FluentPrayerTimes.Show");
                while (true) { show.WaitOne(); window.RequestShow(); }
            }
            catch (Exception e) { Log.Error("show-listener", e); }
        }) { IsBackground = true, Name = "ShowListener" };
        thread.Start();
    }

	private static void StartQuitListener(MainWindow window)
	{
		try
		{
			EventWaitHandle quit = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "FluentPrayerTimes.Quit");
			quit.Reset();
			Thread thread = new Thread((ThreadStart)delegate
			{
				quit.WaitOne();
				window.RequestQuit();
			});
			thread.IsBackground = true;
			thread.Name = "QuitListener";
			thread.Start();
		}
		catch (Exception error)
		{
            Log.Error("app", error);
		}
	}
}
