using System;
using System.CodeDom.Compiler;
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
		base.UnhandledException += delegate(object s, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			try
			{
				Directory.CreateDirectory(AppData);
				File.AppendAllText(Path.Combine(AppData, "crash.log"), DateTime.Now.ToString() + "\n" + e.Exception?.ToString() + "\n\n");
			}
			catch
			{
			}
		};
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		foreach (string text in commandLineArgs)
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
				E2ELog = text.Substring(10);
			}
			if (text.StartsWith("--e2e-set="))
			{
				E2ESet = text.Substring(10);
			}
		}
		if (Environment.GetCommandLineArgs().Any((string a) => a == "--selftest"))
		{
			SelfTest.Run();
			return;
		}
		if (Environment.GetCommandLineArgs().Any((string a) => a == "--update-e2e"))
		{
			SelfTest.RunUpdateE2E();
			return;
		}
		L.Set(PreviewLang ?? Settings.Load().Lang);
		_mutex = new Mutex(initiallyOwned: true, "FluentPrayerTimes.SingleInstance", out var createdNew);
		if (!createdNew)
		{
			Environment.Exit(0);
			return;
		}
		bool startHidden = Environment.GetCommandLineArgs().Any((string a) => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)) || Preview != null;
		_window = new MainWindow(startHidden);
		StartQuitListener(_window);
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
		catch
		{
		}
	}
}
