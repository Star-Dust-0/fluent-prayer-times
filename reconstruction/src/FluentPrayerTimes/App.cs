using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FluentPrayerTimes.FluentPrayerTimes_XamlTypeInfo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using WinRT;
using WinRT.FluentPrayerTimesVtableClasses;

namespace FluentPrayerTimes;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IApplicationOverrides")]
[WinRTExposedType(typeof(FluentPrayerTimes_AppWinRTTypeDetails))]
public class App : Application, IXamlMetadataProvider
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

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	private XamlMetaDataProvider __appProvider;

	public static string AppData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FluentPrayerTimes");

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	[DebuggerNonUserCode]
	private XamlMetaDataProvider _AppProvider
	{
		get
		{
			if (__appProvider == null)
			{
				__appProvider = new XamlMetaDataProvider();
			}
			return __appProvider;
		}
	}

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

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///App.xaml");
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(Type type)
	{
		return _AppProvider.GetXamlType(type);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(string fullName)
	{
		return _AppProvider.GetXamlType(fullName);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2501")]
	[DebuggerNonUserCode]
	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return _AppProvider.GetXmlnsDefinitions();
	}
}
