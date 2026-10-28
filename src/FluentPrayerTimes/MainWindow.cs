using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using WinRT;
using WinRT.Interop;
using Windows.Graphics;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace FluentPrayerTimes;

public sealed partial class MainWindow : Window
{
	private struct CurPt
	{
		public int X;

		public int Y;
	}

	private static readonly (int Id, string Ar)[] Methods = new(int, string)[14]
	{
		(-1, "تلقائي حسب الموقع"),
		(5, "الهيئة العامة المصرية للمساحة"),
		(2, "الاتحاد الإسلامي بأمريكا الشمالية"),
		(3, "رابطة العالم الإسلامي"),
		(4, "جامعة أم القرى"),
		(1, "جامعة العلوم الإسلامية بكراتشي"),
		(12, "اتحاد المنظمات الإسلامية في فرنسا"),
		(8, "منطقة الخليج"),
		(9, "الكويت"),
		(10, "قطر"),
		(11, "سنغافورة"),
		(13, "تركيا"),
		(14, "روسيا"),
		(99, "تحديد يدوي")
	};

	private const int CmdOpen = 1;

	private const int CmdSettings = 2;

	private const int CmdExit = 3;

	private const int CmdWidget = 4;

	private const int CmdUpdate = 5;

	private WidgetWindow? _widget;

	private string _wName = "--";

	private string _wTime = "";

	private string _wCount = "--:--";

	private string _wHeader = L.T("الصلاة القادمة");

	private string _wUnit = "";

	private Color? _wColor;

	private bool _wMsg;

	private readonly Settings _s = Settings.Load();

	private readonly nint _hwnd;

	private readonly TrayIcon _tray;

	private readonly TrayMenuWindow _menu;

	private readonly HoverWindow _hover;

	private string _hTitle = "";

	private string _hCount = "--:--";

	private string _hUnit = "";

	private string _hSub = "";

	private Color? _hColor;

	private bool _hMsg;

	private Brush? _countBrush;

	private string _lastIqama = "";

	private bool _checkingUpdates;

	private int _flyGen;

	private bool _flyBelow = true;

	private Storyboard? _flyMotion;

	private static readonly Color Green = Color.FromArgb(byte.MaxValue, 159, 216, 159);

	private static readonly Color Amber = Color.FromArgb(byte.MaxValue, 234, 163, 0);

	private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;

	private readonly TextBlock[] _rowName = new TextBlock[6];

	private readonly TextBlock[] _rowTime = new TextBlock[6];

	private readonly Border[] _rowBox = new Border[6];

	private List<Times.Place> _places = new List<Times.Place>();

	private readonly List<City> _cities = new List<City>();

	private string _cityCc = "";

	private Times.Result? _res;

	private DateTime _loadedDay = DateTime.MinValue;

	private DateTime _lastTry = DateTime.MinValue;

	private bool _loading = true;

	private bool _quit;

	private int _hl = -2;

	private string _lastAdhan = "";

	private DateTime _azkarNext = DateTime.MaxValue;

	private static readonly string[] AccentSwatches = new string[10] { "#00C4CC", "#0078D4", "#5B5FC7", "#8764B8", "#C239B3", "#E81123", "#F7630C", "#FFB900", "#10893E", "#69797E" };

	private Microsoft.UI.Dispatching.DispatcherQueueTimer? _updFirst;

	private Microsoft.UI.Dispatching.DispatcherQueueTimer? _updTimer;

	private UpdateToast? _toast;

	private bool _navReady;

	private Storyboard? _navMotion;

	private int _reloadGeneration;

	private bool _locateFailed;

	private int _pt;

	private static readonly string[] SoundTags = new string[4] { "default", "short", "full", "none" };

	private const string UpdateRepo = "StarsDust01/fluent-prayer-times";

	private string _updateState = "idle";

	private string? _manualUpdateUrl;

	private string? _manualUpdateVersion;

	private UpdateAsset? _manualUpdateAsset;

	private static readonly UpdateAsset PreviewAsset = new UpdateAsset("https://github.com/StarsDust01/fluent-prayer-times/releases/download/v9.0.0/FluentPrayerTimes-v9.0.0-installer-win-x64.exe", "FluentPrayerTimes-v9.0.0-installer-win-x64.exe", 0L, null);

	private static Color DefaultAccent => Color.FromArgb(byte.MaxValue, 0, 196, 204);

	private void LoadCalculationUi()
	{
		bool loading = _loading;
		_loading = true;
		CalculationControls.Children.Clear();
		Label("ت\u064fختار الإعدادات تلقائي\u064bا حسب الموقع. يمكنك تعديل كل خيار أو إعادته إلى الوضع التلقائي.");
		string text = ((_res?.Today == null) ? null : Methods.FirstOrDefault(((int Id, string Ar) m) => m.Id == _res.Today.Method).Ar);
		if (_s.CalculationMethod == -1 && text != null)
		{
			Label(L.T("الطريقة المستخدمة: ") + L.T(text));
		}
		Choice("المذهب الفقهي لصلاة العصر", new string[3] { "تلقائي حسب الموقع", "الشافعي والمالكي والحنبلي", "الحنفي" }, _s.AsrSchool + 1, delegate(int i)
		{
			_s.AsrSchool = i - 1;
		});
		Choice("طريقة الحساب لصلاتي الفجر والعشاء", Methods.Select(((int Id, string Ar) m) => m.Ar).ToArray(), Math.Max(0, Array.FindIndex(Methods, ((int Id, string Ar) m) => m.Id == _s.CalculationMethod)), delegate(int i)
		{
			_s.CalculationMethod = Methods[i].Id;
		});
		if (_s.CalculationMethod == 99)
		{
			Choice("إعدادات الحساب اليدوي", new string[4] { "زوايا الفجر والعشاء", "زاوية الفجر ودقائق العشاء بعد المغرب", "دقائق الفجر قبل الشروق والعشاء بعد المغرب", "دقائق الفجر قبل الشروق وزاوية العشاء" }, _s.CustomRule, delegate(int i)
			{
				_s.CustomRule = i;
			});
			int customRule = _s.CustomRule;
			if ((uint)customRule <= 1u)
			{
				Number("زاوية الفجر (درجة)", _s.FajrAngle, 1.0, 30.0, delegate(double v)
				{
					_s.FajrAngle = v;
				});
			}
			else
			{
				Number("الفجر قبل الشروق (دقيقة)", _s.FajrMinutes, 1.0, 240.0, delegate(double v)
				{
					_s.FajrMinutes = v;
				});
			}
			customRule = _s.CustomRule;
			if ((customRule == 0 || customRule == 3) ? true : false)
			{
				Number("زاوية العشاء (درجة)", _s.IshaAngle, 1.0, 30.0, delegate(double v)
				{
					_s.IshaAngle = v;
				});
			}
			else
			{
				Number("العشاء بعد المغرب (دقيقة)", _s.IshaMinutes, 1.0, 240.0, delegate(double v)
				{
					_s.IshaMinutes = v;
				});
			}
		}
		Choice("التعديل للمناطق ذات خطوط العرض المرتفعة", new string[5] { "تلقائي حسب الموقع", "لا تعديل", "منتصف الليل", "سبع الليلة", "طريقة الحساب بالزاوية" }, _s.HighLatitude + 1, delegate(int i)
		{
			_s.HighLatitude = i - 1;
		});
		TextBox zone = new TextBox
		{
			Header = L.T("المنطقة الزمنية"),
			PlaceholderText = L.T("تلقائي حسب الموقع"),
			Text = _s.TimeZoneOverride
		};
		Button button = new Button
		{
			Content = L.T("تطبيق")
		};
		button.Click += delegate
		{
			string text2 = zone.Text.Trim();
			try
			{
				if (text2 != "")
				{
					TimeZoneInfo.FindSystemTimeZoneById(text2);
				}
			}
			catch
			{
				zone.Header = L.T("المنطقة الزمنية غير صحيحة");
				return;
			}
			_s.TimeZoneOverride = text2;
			Save();
		};
		CalculationControls.Children.Add(zone);
		CalculationControls.Children.Add(button);
		TextBlock dstTitle = DstTitle;
		Visibility visibility = (DstPanel.Visibility = ((!_s.DstApplies()) ? Visibility.Collapsed : Visibility.Visible));
		dstTitle.Visibility = visibility;
		RadioButton rbDstOff = RbDstOff;
		bool isEnabled = (RbDstOn.IsEnabled = string.IsNullOrEmpty(_s.TimeZoneOverride));
		rbDstOff.IsEnabled = isEnabled;
		_loading = loading;
		ComboBox Choice(string label, string[] items, int selected, Action<int> set)
		{
			Label(label);
			ComboBox combo = new ComboBox
			{
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			foreach (string ar in items)
			{
				combo.Items.Add(L.T(ar));
			}
			combo.SelectedIndex = selected;
			combo.SelectionChanged += delegate
			{
				if (!_loading && combo.SelectedIndex >= 0)
				{
					set(combo.SelectedIndex);
					Save();
					LoadCalculationUi();
				}
			};
			CalculationControls.Children.Add(combo);
			return combo;
		}
		void Label(string ar)
		{
			CalculationControls.Children.Add(new TextBlock
			{
				Text = L.T(ar),
				TextWrapping = TextWrapping.Wrap
			});
		}
		NumberBox Number(string label, double value, double min, double max, Action<double> set)
		{
			NumberBox numberBox = new NumberBox
			{
				Header = L.T(label),
				Value = value,
				Minimum = min,
				Maximum = max,
				SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
			};
			numberBox.ValueChanged += delegate(NumberBox _, NumberBoxValueChangedEventArgs e)
			{
				if (!_loading && !double.IsNaN(e.NewValue))
				{
					set(e.NewValue);
					Save();
				}
			};
			CalculationControls.Children.Add(numberBox);
			return numberBox;
		}
		void Save()
		{
			_s.Save();
			ReloadAsync();
		}
	}

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint h);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint h);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint h, int i);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint h, int i, nint v);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out CurPt p);

	public MainWindow(bool startHidden)
	{
		if (App.E2ESet != null)
		{
			string[] array = App.E2ESet.Split(';', StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=', 2);
				if (array2.Length == 2)
				{
					bool flag = array2[1].Equals("true", StringComparison.OrdinalIgnoreCase);
					if (array2[0] == "AdhanNotify")
					{
						_s.AdhanNotify = flag;
					}
					else if (array2[0] == "IqamaNotify")
					{
						_s.IqamaNotify = flag;
					}
					else if (array2[0] == "NotifyAdhan")
					{
						_s.NotifyAdhan = flag;
					}
				}
			}
		}
		ApplyAccentResources(ParseHex(_s.Accent));
		InitializeComponent();
        Nav.SelectionChanged += Nav_SelectionChanged;
        BtnApply.Click += ApplyChanges_Click;
        CmbLang.SelectionChanged += Lang_Changed;
        RbMicaAlt.Checked += Backdrop_Checked;
        RbMica.Checked += Backdrop_Checked;
        RbAcrylic.Checked += Backdrop_Checked;
        RbThemeSys.Checked += Theme_Checked;
        RbThemeLight.Checked += Theme_Checked;
        RbThemeDark.Checked += Theme_Checked;
        BtnAccentCustom.Click += AccentCustom_Click;
        BtnAccentReset.Click += AccentReset_Click;
        CmbCountry.SelectionChanged += Country_Changed;
        CmbCity.SelectionChanged += City_Changed;
        LstPlaces.SelectionChanged += Place_Selected;
        RbDstAuto.Checked += Dst_Checked;
        RbDstOff.Checked += Dst_Checked;
        RbDstOn.Checked += Dst_Checked;
        TglWidget.Toggled += Widget_Toggled;
        TglAdhan.Toggled += Adhan_Toggled;
        TglAutoUpdate.Toggled += AutoUpdate_Toggled;
        BtnUpdate.Click += Update_Click;
        BtnUpdateInstall.Click += UpdateInstall_Click;
        TglStartup.Toggled += Startup_Toggled;
        BtnAboutUpdate.Click += Update_Click;
        BtnAboutUpdateInstall.Click += UpdateInstall_Click;
		L.Register(RootGrid);
		ApplyFlow();
		L.Refresh();
		RefreshUpdateCards();
		if (App.Preview != null)
		{
			_s.WidgetVisible = false;
		}
		if (App.Preview != null)
		{
			_s.LocationPending = App.PreviewPart?.StartsWith("nolocation") ?? false;
			_locateFailed = _s.LocationPending;
		}
		_hwnd = WindowNative.GetWindowHandle(this);
		base.Title = L.T("مواقيت الصلاة - Fluent Prayer Times");
		base.ExtendsContentIntoTitleBar = true;
		SetTitleBar(TitleBarArea);
		if (base.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.IsMaximizable = false;
			overlappedPresenter.IsMinimizable = false;
		}
		double num = (double)GetDpiForWindow(_hwnd) / 96.0;
		if (num < 1.0)
		{
			num = 1.0;
		}
		base.AppWindow.Resize(new SizeInt32((int)(470.0 * num), (int)(748.0 * num)));
		try
		{
			base.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "app.ico"));
		}
		catch
		{
		}
		base.AppWindow.Closing += delegate(AppWindow s, AppWindowClosingEventArgs a)
		{
			if (!_quit)
			{
				a.Cancel = true;
				HideFlyout();
			}
		};
		HideFromTaskbar();
		RootGrid.RenderTransform = new TranslateTransform();
		_countBrush = TxtCountdown.Foreground;
		try
		{
			ImgTitle.Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "tray.png")));
			ImgLogo.Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "logo.png")));
			Version version = typeof(App).Assembly.GetName().Version;
			if (version != null)
			{
				TxtVersion.Text = L.T("الإصدار ") + version.Major + "." + version.Minor + "." + version.Build;
			}
		}
		catch
		{
		}
		RootGrid.SizeChanged += delegate
		{
			FitPages();
		};
		BuildRows();
		PanelCalendar.Children.Add(new CalendarPage());
		PanelConvert.Children.Add(new ConverterPage());
		PanelAzkar.Children.Add(new AzkarPage(_s, OnAzkarChanged));
		_azkarNext = (_s.AzkarOn ? DateTime.Now.AddMinutes(Math.Max(1, _s.AzkarMinutes)) : DateTime.MaxValue);
		LoadSettingsUi();
		ApplyAppearance();
		Nav.SelectedItem = Nav.MenuItems[0];
		_loading = false;
		if (_s.LocationPending && App.Preview == null)
		{
			AutoLocateFirstRun();
		}
		_menu = new TrayMenuWindow(OnMenu);
		_tray = new TrayIcon(_hwnd, TrayIconPath(), new(int, string)[5]
		{
			(1, L.T("فتح")),
			(4, L.T("ويدجت سطح المكتب")),
			(2, L.T("الإعدادات")),
			(5, L.T("التحقق من التحديثات")),
			(3, L.T("خروج"))
		}, ToggleFlyout, OnMenu, OnContext);
		_tray.OnSystemThemeChanged = delegate
		{
			_tray.SetIcon(TrayIconPath());
		};
		_hover = new HoverWindow(delegate
		{
			if (!_tray.TryGetAnchor(out var x, out var y) || !GetCursorPos(out var p))
			{
				return false;
			}
			double num2 = Math.Max(1.0, (double)GetDpiForWindow(_hwnd) / 96.0);
			return (double)Math.Abs(p.X - x) <= 18.0 * num2 && (double)Math.Abs(p.Y - y) <= 18.0 * num2;
		});
		_tray.OnHover = delegate
		{
			_hover.RequestShow(delegate
			{
				if (!_menu.AppWindow.IsVisible && !base.AppWindow.IsVisible && _tray.TryGetAnchor(out var x, out var y))
				{
					_hover.Update(_hTitle, _hCount, _hUnit, _hSub, _hColor, _hMsg);
					_hover.ShowAt(x, y, _s.Theme);
				}
			});
		};
		_timer = base.DispatcherQueue.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1.0);
		_timer.Tick += delegate
		{
			Tick();
		};
		_timer.Start();
		ReloadAsync();
		if (_s.LocationPending)
		{
			SyncCitySelection();
		}
		if (App.Preview != null && _s.LocationPending)
		{
			Microsoft.UI.Dispatching.DispatcherQueueTimer dispatcherQueueTimer = base.DispatcherQueue.CreateTimer();
			dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1.5);
			dispatcherQueueTimer.IsRepeating = false;
			dispatcherQueueTimer.Tick += delegate
			{
				if (App.PreviewPart == "nolocation-settings")
				{
					Nav.SelectedItem = Nav.FooterMenuItems[0];
				}
				ShowFlyout(nearCursor: false);
			};
			dispatcherQueueTimer.Start();
		}
		if (_s.WidgetVisible)
		{
			SetWidget(on: true);
		}
		if (!startHidden)
		{
			ShowFlyout(nearCursor: false);
		}
		StartUpdateWatch();
	}

	private bool OnContext(int x, int y)
	{
		try
		{
			_hover.Hide();
			if (_tray.TryGetAnchor(out var x2, out var y2))
			{
				x = x2;
				y = y2;
			}
			_menu.ShowAt(x, y, _s.Theme, _s.WidgetVisible);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void OnMenu(int cmd)
	{
		switch (cmd)
		{
		case 1:
			Nav.SelectedItem = Nav.MenuItems[0];
			ShowFlyout(nearCursor: false);
			break;
		case 2:
			Nav.SelectedItem = Nav.FooterMenuItems[0];
			ShowFlyout(nearCursor: false);
			break;
		case 4:
			SetWidget(!_s.WidgetVisible);
			break;
		case 5:
			Nav.SelectedItem = Nav.FooterMenuItems[0];
			ShowFlyout(nearCursor: false);
			CheckForUpdatesAsync();
			break;
		case 3:
			Quit();
			break;
		}
	}

	private void SetWidget(bool on)
	{
		_s.WidgetVisible = on;
		_s.Save();
		if (on)
		{
			if (_widget == null)
			{
				_widget = new WidgetWindow(_s);
				_widget.HiddenByUser = delegate
				{
					_s.WidgetVisible = false;
					_s.Save();
					SyncWidgetToggle();
				};
			}
			_widget.SetInfo(_wName, _wTime, _wCount, _wHeader, _wColor, _wMsg, _wUnit);
			_widget.ShowWidget();
		}
		else
		{
			_widget?.HideWidget();
		}
		SyncWidgetToggle();
	}

	private void SyncWidgetToggle()
	{
		bool loading = _loading;
		_loading = true;
		TglWidget.IsOn = _s.WidgetVisible;
		_loading = loading;
	}

	private void Widget_Toggled(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			SetWidget(TglWidget.IsOn);
		}
	}

	private void ApplyChanges_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_s.Save();
			string processPath = Environment.ProcessPath;
			if (!string.IsNullOrEmpty(processPath))
			{
				Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 3 >nul & start \"\" \"" + processPath + "\"")
				{
					CreateNoWindow = true,
					UseShellExecute = false
				});
			}
		}
		catch
		{
		}
		Quit();
	}

	public void RequestQuit()
	{
		if (!base.DispatcherQueue.TryEnqueue(Quit))
		{
			Environment.Exit(0);
		}
	}

	private void Quit()
	{
		_quit = true;
		_hover.Hide();
		_timer.Stop();
		_tray.Dispose();
		Application.Current.Exit();
		Environment.Exit(0);
	}

	private void HideFromTaskbar()
	{
		try
		{
			base.AppWindow.IsShownInSwitchers = false;
			long num = ((IntPtr)GetWindowLongPtr(_hwnd, -20)).ToInt64();
			num = (num | 0x80) & -262145;
			SetWindowLongPtr(_hwnd, -20, new IntPtr(num));
		}
		catch
		{
		}
	}

	private void ToggleFlyout()
	{
		_hover.Hide();
		OverlappedPresenter obj = base.AppWindow.Presenter as OverlappedPresenter;
		bool flag = (object)obj != null && obj.State == OverlappedPresenterState.Minimized;
		if (base.AppWindow.IsVisible && !flag)
		{
			HideFlyout();
			return;
		}
		Nav.SelectedItem = Nav.MenuItems[0];
		ShowFlyout(nearCursor: true);
	}

	private async void HideFlyout()
	{
		if (base.AppWindow.IsVisible)
		{
			int g = ++_flyGen;
			await AnimateFlyout(show: false);
			if (g == _flyGen)
			{
				base.AppWindow.Hide();
				RootGrid.Opacity = 1.0;
				RootGrid.RenderTransform = new TranslateTransform();
			}
		}
	}

	private Task AnimateFlyout(bool show)
	{
		_flyMotion?.Stop();
		if (!new UISettings().AnimationsEnabled)
		{
			RootGrid.Opacity = 1.0;
			RootGrid.RenderTransform = new TranslateTransform();
			return Task.CompletedTask;
		}
		TranslateTransform translateTransform = new TranslateTransform();
		RootGrid.RenderTransform = translateTransform;
		double num = (show ? 220 : 170);
		double num2 = (_flyBelow ? 42 : (-42));
		CubicEase easingFunction = new CubicEase
		{
			EasingMode = ((!show) ? EasingMode.EaseIn : EasingMode.EaseOut)
		};
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			From = (show ? num2 : 0.0),
			To = (show ? 0.0 : num2),
			Duration = new Duration(TimeSpan.FromMilliseconds(num)),
			EasingFunction = easingFunction,
			EnableDependentAnimation = true
		};
		DoubleAnimation doubleAnimation2 = new DoubleAnimation
		{
			From = ((!show) ? 1 : 0),
			To = (show ? 1 : 0),
			Duration = new Duration(TimeSpan.FromMilliseconds(num)),
			EasingFunction = easingFunction
		};
		Storyboard.SetTarget(doubleAnimation, translateTransform);
		Storyboard.SetTargetProperty(doubleAnimation, "Y");
		Storyboard.SetTarget(doubleAnimation2, RootGrid);
		Storyboard.SetTargetProperty(doubleAnimation2, "Opacity");
		Storyboard storyboard = (_flyMotion = new Storyboard());
		storyboard.Children.Add(doubleAnimation);
		storyboard.Children.Add(doubleAnimation2);
		storyboard.Begin();
		return Task.Delay((int)num);
	}

	private void ShowFlyout(bool nearCursor)
	{
		_flyGen++;
		if (base.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } overlappedPresenter)
		{
			overlappedPresenter.Restore();
		}
		bool isVisible = base.AppWindow.IsVisible;
		Place(nearCursor);
		HideFromTaskbar();
		if (!isVisible)
		{
			RootGrid.Opacity = 0.0;
		}
		if (!base.AppWindow.IsVisible)
		{
			base.AppWindow.Show();
		}
		Activate();
		SetForegroundWindow(_hwnd);
		if (!isVisible)
		{
			AnimateFlyout(show: true);
			return;
		}
		RootGrid.Opacity = 1.0;
		RootGrid.RenderTransform = new TranslateTransform();
	}

	private void Place(bool nearCursor)
	{
		try
		{
			int x;
			int y;
			bool flag = _tray.TryGetAnchor(out x, out y);
			if (!flag && nearCursor && GetCursorPos(out var p))
			{
				x = p.X;
				y = p.Y;
				flag = true;
			}
			RectInt32 workArea = (flag ? DisplayArea.GetFromPoint(new PointInt32(x, y), DisplayAreaFallback.Nearest) : DisplayArea.Primary).WorkArea;
			int num = base.AppWindow.Size.Width;
			int num2 = base.AppWindow.Size.Height;
			if (num2 > workArea.Height - 16)
			{
				num2 = workArea.Height - 16;
			}
			if (num > workArea.Width - 16)
			{
				num = workArea.Width - 16;
			}
			int x2;
			int y2;
			if (flag)
			{
				x2 = Math.Clamp(x - num / 2, workArea.X + 8, workArea.X + workArea.Width - num - 8);
				y2 = ((y > workArea.Y + workArea.Height / 2) ? (workArea.Y + workArea.Height - num2 - 8) : (workArea.Y + 8));
				_flyBelow = y > workArea.Y + workArea.Height / 2;
			}
			else
			{
				x2 = workArea.X + workArea.Width - num - 12;
				y2 = workArea.Y + workArea.Height - num2 - 12;
			}
			base.AppWindow.MoveAndResize(new RectInt32(x2, y2, num, num2));
		}
		catch
		{
		}
	}

	private void ApplyAppearance()
	{
		string backdrop = _s.Backdrop;
		SystemBackdrop systemBackdrop = ((backdrop == "mica") ? new MicaBackdrop
		{
			Kind = MicaKind.Base
		} : ((!(backdrop == "acrylic")) ? ((SystemBackdrop)new MicaBackdrop
		{
			Kind = MicaKind.BaseAlt
		}) : ((SystemBackdrop)new DesktopAcrylicBackdrop())));
		base.SystemBackdrop = systemBackdrop;
		Grid rootGrid = RootGrid;
		backdrop = _s.Theme;
		ElementTheme requestedTheme = ((backdrop == "light") ? ElementTheme.Light : ((backdrop == "dark") ? ElementTheme.Dark : ElementTheme.Default));
		rootGrid.RequestedTheme = requestedTheme;
	}

	private static Color ParseHex(string? hex)
	{
		try
		{
			string text = (hex ?? "").TrimStart('#');
			if (text.Length == 6)
			{
				return Color.FromArgb(byte.MaxValue, Convert.ToByte(text.Substring(0, 2), 16), Convert.ToByte(text.Substring(2, 2), 16), Convert.ToByte(text.Substring(4), 16));
			}
		}
		catch
		{
		}
		return DefaultAccent;
	}

	private static Color Mix(Color c, byte tr, byte tg, byte tb, double t)
	{
		return Color.FromArgb(byte.MaxValue, (byte)((double)(int)c.R + (double)(tr - c.R) * t), (byte)((double)(int)c.G + (double)(tg - c.G) * t), (byte)((double)(int)c.B + (double)(tb - c.B) * t));
	}

	private static void ApplyAccentResources(Color c)
	{
		ResourceDictionary resources = Application.Current.Resources;
		resources["SystemAccentColor"] = c;
		resources["SystemAccentColorDark1"] = Mix(c, 0, 0, 0, 0.18);
		resources["SystemAccentColorDark2"] = Mix(c, 0, 0, 0, 0.36);
		resources["SystemAccentColorDark3"] = Mix(c, 0, 0, 0, 0.54);
		resources["SystemAccentColorLight1"] = Mix(c, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0.18);
		resources["SystemAccentColorLight2"] = Mix(c, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0.36);
		resources["SystemAccentColorLight3"] = Mix(c, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0.54);
	}

	private void RefreshAccent()
	{
		try
		{
			Flip(RootGrid);
		}
		catch
		{
		}
		try
		{
			_widget?.RefreshTheme();
		}
		catch
		{
		}
		try
		{
			_hover.RefreshTheme();
		}
		catch
		{
		}
		static void Flip(FrameworkElement fe)
		{
			ElementTheme requestedTheme = fe.RequestedTheme;
			fe.RequestedTheme = ((requestedTheme == ElementTheme.Dark) ? ElementTheme.Light : ElementTheme.Dark);
			fe.RequestedTheme = requestedTheme;
		}
	}

	private void SetAccent(Color c, bool save)
	{
		ApplyAccentResources(c);
		AccentSwatch.Background = new SolidColorBrush(c);
		if (save)
		{
			_s.Accent = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
			_s.Save();
		}
		RefreshAccent();
	}

	private void InitAccentUi()
	{
		AccentSwatch.Background = new SolidColorBrush(ParseHex(_s.Accent));
		int num = 0;
		string[] accentSwatches = AccentSwatches;
		for (int i = 0; i < accentSwatches.Length; i++)
		{
			Color color = ParseHex(accentSwatches[i]);
			Button button = new Button
			{
				Width = 40.0,
				Height = 40.0,
				Padding = new Thickness(0.0),
				CornerRadius = new CornerRadius(4.0),
				Background = new SolidColorBrush(color),
				Tag = color
			};
			button.Click += delegate(object a, RoutedEventArgs e)
			{
				SetAccent((Color)((Button)a).Tag, save: true);
			};
			((num++ < 5) ? AccentPresets : AccentPresets2).Children.Add(button);
		}
	}

	private async void AccentCustom_Click(object sender, RoutedEventArgs e)
	{
		ColorPicker colorPicker = new ColorPicker
		{
			Color = ParseHex(_s.Accent),
			IsAlphaEnabled = false,
			IsMoreButtonVisible = true,
			IsColorChannelTextInputVisible = true,
			IsHexInputVisible = true,
			FlowDirection = FlowDirection.LeftToRight
		};
		colorPicker.ColorChanged += delegate(ColorPicker s2, ColorChangedEventArgs a2)
		{
			SetAccent(a2.NewColor, save: true);
		};
		ContentDialog contentDialog = new ContentDialog
		{
			XamlRoot = RootGrid.XamlRoot,
			Title = L.T("لون مخصص"),
			Content = new ScrollViewer
			{
				Content = colorPicker,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
			},
			CloseButtonText = L.T("تم"),
			DefaultButton = ContentDialogButton.Close,
			FlowDirection = L.Flow
		};
		try
		{
			await contentDialog.ShowAsync();
		}
		catch
		{
		}
	}

	private void AccentReset_Click(object sender, RoutedEventArgs e)
	{
		SetAccent(DefaultAccent, save: true);
		_s.Accent = "";
		_s.Save();
	}

	private void StartUpdateWatch()
	{
		if (App.Preview == null)
		{
			_updFirst = base.DispatcherQueue.CreateTimer();
			_updFirst.Interval = TimeSpan.FromSeconds(45.0);
			_updFirst.IsRepeating = false;
			_updFirst.Tick += async delegate
			{
				await AutoUpdateAsync();
			};
			_updFirst.Start();
			_updTimer = base.DispatcherQueue.CreateTimer();
			_updTimer.Interval = TimeSpan.FromHours(6.0);
			_updTimer.Tick += async delegate
			{
				await AutoUpdateAsync();
			};
			_updTimer.Start();
		}
	}

	private async Task AutoUpdateAsync()
	{
		if (!_s.AutoUpdate || _quit || _checkingUpdates)
		{
			return;
		}
		try
		{
			using HttpClient http = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20.0)
			};
			http.DefaultRequestHeaders.UserAgent.ParseAdd("FluentPrayerTimes/" + (typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"));
			http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
			using HttpResponseMessage response = await http.GetAsync("https://api.github.com/repos/StarsDust01/fluent-prayer-times/releases/latest");
			response.EnsureSuccessStatusCode();
			using JsonDocument jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
			Version installed = ReleaseVersion(typeof(MainWindow).Assembly.GetName().Version?.ToString(3)) ?? new Version(0, 0, 0);
			UpdateAsset updateAsset = UpdateAssetFor(jsonDocument.RootElement, installed);
			if (updateAsset == null || _quit)
			{
				return;
			}
			string text = (jsonDocument.RootElement.GetProperty("tag_name").GetString() ?? "").TrimStart('v', 'V');
			if (_s.UpdateSeen == text)
			{
				return;
			}
			_s.UpdateSeen = text;
			_s.Save();
			_toast = new UpdateToast(text, updateAsset, "https://github.com/StarsDust01/fluent-prayer-times/releases/latest", _s.Theme);
			_toast.ShowToast();
		}
		catch (Exception ex)
		{
			try
			{
				Directory.CreateDirectory(App.AppData);
				File.AppendAllText(Path.Combine(App.AppData, "update.log"), DateTime.Now.ToString() + " auto: " + ex.Message + "\n");
			}
			catch
			{
			}
		}
	}

	private void AutoUpdate_Toggled(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			_s.AutoUpdate = TglAutoUpdate.IsOn;
			_s.Save();
		}
	}

	private void FitPages()
	{
		double num = RootGrid.ActualWidth - 48.0;
		if (num < 200.0)
		{
			num = 200.0;
		}
		double width = Math.Max(200.0, Math.Min(560.0, num - 32.0));
		ScrollViewer[] array = new ScrollViewer[6] { PageTimes, PageSettings, PageAbout, PageCalendar, PageConvert, PageAzkar };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].MaxWidth = num;
		}
		PanelTimes.Width = width;
		PanelSettings.Width = width;
		PanelAbout.Width = width;
		PanelCalendar.Width = width;
		PanelConvert.Width = width;
		PanelAzkar.Width = width;
	}

	private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
        if (PageTimes == null || PageSettings == null || PageAbout == null) return;
		string text = (args.SelectedItemContainer?.Tag as string) ?? "times";
		PageTimes.Visibility = ((!(text == "times")) ? Visibility.Collapsed : Visibility.Visible);
		PageSettings.Visibility = ((!(text == "settings")) ? Visibility.Collapsed : Visibility.Visible);
		PageAbout.Visibility = ((!(text == "about")) ? Visibility.Collapsed : Visibility.Visible);
		PageCalendar.Visibility = ((!(text == "calendar")) ? Visibility.Collapsed : Visibility.Visible);
		PageConvert.Visibility = ((!(text == "convert")) ? Visibility.Collapsed : Visibility.Visible);
		PageAzkar.Visibility = ((!(text == "azkar")) ? Visibility.Collapsed : Visibility.Visible);
		FitPages();
		ScrollViewer page = text switch
		{
			"settings" => PageSettings, 
			"about" => PageAbout, 
			"calendar" => PageCalendar, 
			"convert" => PageConvert, 
			"azkar" => PageAzkar, 
			_ => PageTimes, 
		};
		if (_navReady)
		{
			SlideIn(page);
		}
		_navReady = true;
	}

	private void SlideIn(FrameworkElement page)
	{
		_navMotion?.Stop();
		if (!new UISettings().AnimationsEnabled)
		{
			page.Opacity = 1.0;
			page.RenderTransform = new TranslateTransform();
			return;
		}
		TranslateTransform target = (TranslateTransform)(page.RenderTransform = new TranslateTransform());
		CubicEase easingFunction = new CubicEase
		{
			EasingMode = EasingMode.EaseOut
		};
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			From = 96.0,
			To = 0.0,
			Duration = new Duration(TimeSpan.FromMilliseconds(320.0)),
			EasingFunction = easingFunction,
			EnableDependentAnimation = true
		};
		DoubleAnimation doubleAnimation2 = new DoubleAnimation
		{
			From = 0.0,
			To = 1.0,
			Duration = new Duration(TimeSpan.FromMilliseconds(220.0)),
			EasingFunction = easingFunction
		};
		Storyboard.SetTarget(doubleAnimation, target);
		Storyboard.SetTargetProperty(doubleAnimation, "Y");
		Storyboard.SetTarget(doubleAnimation2, page);
		Storyboard.SetTargetProperty(doubleAnimation2, "Opacity");
		Storyboard storyboard = (_navMotion = new Storyboard());
		page.Opacity = 0.0;
		storyboard.Children.Add(doubleAnimation);
		storyboard.Children.Add(doubleAnimation2);
		storyboard.Begin();
	}

	private static string TrayIconPath()
	{
		return Path.Combine(AppContext.BaseDirectory, TaskbarIsLight() ? "tray-dark.ico" : "tray-white.ico");
	}

	private static bool TaskbarIsLight()
	{
		try
		{
			return Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", 0) is int num && num == 1;
		}
		catch
		{
			return false;
		}
	}

	private void BuildRows()
	{
		for (int i = 0; i < 6; i++)
		{
			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			TextBlock textBlock = new TextBlock
			{
				Text = Times.Names[i],
				FontSize = 16.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock textBlock2 = new TextBlock
			{
				Text = "--:--",
				FontSize = 18.0,
				FlowDirection = FlowDirection.LeftToRight,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(textBlock, 0);
			Grid.SetColumn(textBlock2, 1);
			grid.Children.Add(textBlock);
			grid.Children.Add(textBlock2);
			Border border = new Border
			{
				Child = grid,
				Padding = new Thickness(14.0, 6.0, 14.0, 6.0),
				CornerRadius = new CornerRadius(6.0),
				BorderThickness = new Thickness(1.0),
				BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
			};
			_rowName[i] = textBlock;
			_rowTime[i] = textBlock2;
			_rowBox[i] = border;
			RowsPanel.Children.Add(border);
		}
	}

	private async Task ReloadAsync()
	{
		int generation = ++_reloadGeneration;
		if (_s.LocationPending)
		{
			_res = new Times.Result(null, null, Offline: false);
			TxtCity.Text = L.T("الموقع غير محدد");
			TxtDates.Text = "";
			TxtStatus.Text = (_locateFailed ? L.T("تعذ\u0651ر تحديد موقعك تلقائي\u064bا. اختر دولتك ومدينتك من الإعدادات.") : L.T("جار\u064d تحديد موقعك..."));
			_hl = -2;
			Tick();
			return;
		}
		Settings s = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(_s));
		_lastTry = DateTime.Now;
		Times.Result result;
		try
		{
			result = await Times.Load(s);
		}
		catch
		{
			return;
		}
		if (generation != _reloadGeneration)
		{
			return;
		}
		if (result.Today?.Tz != null)
		{
			_s.Location.Tz = result.Today.Tz;
			_s.Save();
		}
		_res = result;
		if (result.Today != null)
		{
			_loadedDay = Times.Now(_s.Location.Tz).Date;
			for (int i = 0; i < 6; i++)
			{
				_rowTime[i].Text = Times.F12(result.Today.T[Times.Keys[i]]);
			}
		}
		ShowDatesAndCity(result);
		_hl = -2;
		Tick();
	}

	private void OnAzkarChanged()
	{
		_azkarNext = (_s.AzkarOn ? DateTime.Now.AddMinutes(Math.Max(1, _s.AzkarMinutes)) : DateTime.MaxValue);
	}

	private void Adhan_Toggled(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			_s.NotifyAdhan = TglAdhan.IsOn;
			_s.Save();
		}
	}

	private void ShowDatesAndCity(Times.Result r)
	{
		TxtCity.Text = _s.Location.Display;
		DateTime dateTime = Times.Now(_s.Location.Tz);
		string text = (L.IsArabic ? dateTime.ToString("dddd d MMMM yyyy", new CultureInfo("ar-EG")) : (HijriUtil.Days[(int)dateTime.DayOfWeek] + " " + dateTime.Day + " " + HijriUtil.GMonths[dateTime.Month - 1] + " " + dateTime.Year));
		TxtDates.Text = ((r.Today != null) ? (text + "  -  " + L.LocHijri(r.Today.Hijri)) : text);
		TxtStatus.Text = ((r.Today == null) ? L.T("لا يوجد اتصال ولا مواقيت محفوظة") : (r.Offline ? L.T("دون اتصال - من آخر بيانات محفوظة") : ""));
	}

	private void Notify2(string kind, string title, string body, bool silent)
	{
		if (App.E2ELog != null)
		{
			File.AppendAllText(App.E2ELog, kind + "|" + body + "\n");
		}
		else
		{
			_tray.Notify(title, body, silent);
		}
	}

	private void Tick()
	{
		if (DateTime.Now >= _azkarNext)
		{
			_azkarNext = DateTime.Now.AddMinutes(Math.Max(1, _s.AzkarMinutes));
			try
			{
				_tray.Notify(L.T("منبه الأذكار"), AzkarData.RandomShort().Text);
			}
			catch
			{
			}
		}
		DateTime dateTime = Times.Now(_s.Location.Tz);
		if ((_loadedDay != dateTime.Date || _res?.Today == null) && !_s.LocationPending && (dateTime - _lastTry).TotalSeconds > (double)((_res?.Today == null) ? 300 : 5))
		{
			ReloadAsync();
		}
		if (App.E2EClock != null && _res?.Today != null)
		{
			dateTime = Times.At(dateTime, _res.Today.T[Times.Keys[2]]).AddSeconds(5 + ((App.E2EClock == "iqama") ? (60 * _s.IqamaFor(2)) : 0));
		}
		if (App.Preview != null && !_s.LocationPending)
		{
			PreviewTick(dateTime);
			return;
		}
		if (_res?.Today == null)
		{
			TxtNextLabel.Text = L.T("الصلاة القادمة");
			TxtNextName.Text = "--";
			TxtNextTime.Text = "";
			SetMainCount("--:--", "", null, msg: false);
			SetHover(L.T("مواقيت الصلاة"), "--:--", "", _s.Location.Display, null, msg: false);
			PushWidget("--", "", "--:--", L.T("الصلاة القادمة"), null, msg: false);
			return;
		}
		if (_s.NotifyAdhan)
		{
			for (int i = 0; i < Times.Keys.Length; i++)
			{
				if (i == 1)
				{
					continue;
				}
				DateTime dateTime2 = Times.At(dateTime, _res.Today.T[Times.Keys[i]]);
				string text = dateTime.ToString("yyyyMMdd") + "-" + i;
				if (_s.AdhanOn && dateTime >= dateTime2 && (dateTime - dateTime2).TotalSeconds < 90.0 && text != _lastAdhan)
				{
					_lastAdhan = text;
					bool flag = _s.AdhanSound == "none";
					bool flag2 = Sounds.Available("adhan", _s.AdhanSound);
					try
					{
						Notify2("adhan", L.T("مواقيت الصلاة"), L.T("حان الآن موعد أذان ") + Times.Names[i], flag2 || flag);
					}
					catch
					{
					}
					if (flag2)
					{
						Sounds.Play("adhan", _s.AdhanSound);
					}
				}
				DateTime dateTime3 = dateTime2.AddMinutes(_s.IqamaFor(i));
				string text2 = dateTime.ToString("yyyyMMdd") + "-i" + i;
				if (_s.IqamaOn && dateTime >= dateTime3 && (dateTime - dateTime3).TotalSeconds < 90.0 && text2 != _lastIqama)
				{
					_lastIqama = text2;
					bool flag3 = _s.IqamaSound == "none";
					bool flag4 = Sounds.Available("iqama", _s.IqamaSound);
					try
					{
						Notify2("iqama", L.T("مواقيت الصلاة"), L.T("حان وقت إقامة صلاة ") + Times.Names[i], flag4 || flag3);
					}
					catch
					{
					}
					if (flag4)
					{
						Sounds.Play("iqama", _s.IqamaSound);
					}
				}
			}
		}
		Times.Disp disp = Times.State(dateTime, _res.Today.T, _res.Tomorrow?.T, _s);
		if (disp == null)
		{
			TxtNextLabel.Text = L.T("الصلاة القادمة");
			TxtNextName.Text = "--";
			SetMainCount("--:--", "", null, msg: false);
			SetHover(L.T("مواقيت الصلاة"), "--:--", "", _s.Location.Display, null, msg: false);
			PushWidget("--", "", "--:--", L.T("الصلاة القادمة"), null, msg: false);
		}
		else
		{
			Render(disp, Times.F12(_res.Today.T[Times.Keys[disp.Idx]]));
		}
	}

	private void Render(Times.Disp d, string adhanTime)
	{
		string text = Times.Names[d.Idx] + (d.Tomorrow ? L.T(" (غد\u064bا)") : "");
		string text2 = d.Time.ToString("h:mm tt", CultureInfo.InvariantCulture);
		string text3 = L.T("الصلاة القادمة");
		Color? color = null;
		bool msg = false;
		string text4;
		string unit;
		string title;
		switch (d.Phase)
		{
		case Times.Phase.Adhan:
			text3 = L.T("الصلاة الحالية");
			text4 = L.T("حان وقت الاذان");
			unit = "";
			color = Green;
			msg = true;
			title = L.T("موعد صلاة ") + Times.Names[d.Idx];
			text2 = adhanTime;
			break;
		case Times.Phase.Iqama:
			text3 = L.T("الإقامة");
			text4 = Times.FmtMinutes(d.Left);
			unit = L.T("دقيقة");
			color = Amber;
			title = L.T("باقي على إقامة صلاة ") + Times.Names[d.Idx];
			break;
		case Times.Phase.IqamaNow:
			text3 = L.T("الصلاة الحالية");
			text4 = L.T("حان وقت الاقامة");
			unit = "";
			color = Green;
			msg = true;
			title = L.T("إقامة صلاة ") + Times.Names[d.Idx];
			break;
		default:
			text4 = Times.FmtHM(d.Left);
			unit = L.T("ساعة");
			title = ((d.Idx == 1) ? L.T("باقي على الشروق") : (L.T("باقي على صلاة ") + text));
			break;
		}
		TxtNextLabel.Text = text3;
		TxtNextName.Text = text;
		TxtNextTime.Text = text2;
		SetMainCount(text4, unit, color, msg);
		PushWidget(text, text2, text4, text3, color, msg, unit);
		SetHover(title, text4, unit, text + " " + text2 + "  -  " + _s.Location.Display, color, msg);
		int num = (d.Tomorrow ? (-1) : d.Idx);
		if (num != _hl)
		{
			_hl = num;
			for (int i = 0; i < 6; i++)
			{
				bool flag = i == num;
				_rowBox[i].Background = (flag ? new SolidColorBrush(Color.FromArgb(40, 0, 196, 204)) : new SolidColorBrush(Microsoft.UI.Colors.Transparent));
				_rowBox[i].BorderBrush = (flag ? new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, 196, 204)) : new SolidColorBrush(Microsoft.UI.Colors.Transparent));
				_rowName[i].FontWeight = (flag ? FontWeights.SemiBold : FontWeights.Normal);
				_rowTime[i].FontWeight = _rowName[i].FontWeight;
			}
		}
	}

	private void PreviewTick(DateTime now)
	{
		DateTime time = now.Date.AddHours(15.0).AddMinutes(30.0);
		Render(App.Preview switch
		{
			"adhan" => new Times.Disp(Times.Phase.Adhan, 3, time, Tomorrow: false, TimeSpan.FromSeconds(40.0)), 
			"iqama" => new Times.Disp(Times.Phase.Iqama, 3, time.AddMinutes(20.0), Tomorrow: false, TimeSpan.FromSeconds(754.0)), 
			"iqamanow" => new Times.Disp(Times.Phase.IqamaNow, 3, time.AddMinutes(20.0), Tomorrow: false, TimeSpan.Zero), 
			_ => new Times.Disp(Times.Phase.Next, 3, time, Tomorrow: false, TimeSpan.FromMinutes(125.0)), 
		}, "3:30 PM");
		_pt++;
		string text = App.PreviewPart ?? "main";
		if (_pt == 1)
		{
			if (text == "main")
			{
				ShowFlyout(nearCursor: false);
			}
			if (text == "widget")
			{
				SetWidget(on: true);
			}
			if (text == "calculation")
			{
				Nav.SelectedItem = Nav.FooterMenuItems[0];
				ShowFlyout(nearCursor: false);
			}
			if (text == "settings")
			{
				SetAccent(ParseHex("#C239B3"), save: false);
				Nav.SelectedItem = Nav.FooterMenuItems[0];
				ExpAccent.IsExpanded = true;
				ShowFlyout(nearCursor: false);
			}
			if (text == "sounds" || text == "sounds-closed")
			{
				Nav.SelectedItem = Nav.FooterMenuItems[0];
				ShowFlyout(nearCursor: false);
			}
			if (text == "calendar")
			{
				foreach (object menuItem in Nav.MenuItems)
				{
					if (menuItem is NavigationViewItem navigationViewItem && navigationViewItem.Tag as string == "calendar")
					{
						Nav.SelectedItem = navigationViewItem;
					}
				}
				ShowFlyout(nearCursor: false);
			}
			if (text == "about")
			{
				Nav.SelectedItem = Nav.FooterMenuItems[1];
				ShowFlyout(nearCursor: false);
			}
			if (text == "picker")
			{
				SetAccent(ParseHex("#C239B3"), save: false);
				Nav.SelectedItem = Nav.FooterMenuItems[0];
				ShowFlyout(nearCursor: false);
			}
			if (text == "update-live")
			{
				LiveUpdatePreviewAsync();
			}
			else if (text.StartsWith("update") && !text.StartsWith("updates"))
			{
				_toast = new UpdateToast("1.2.5", PreviewAsset, "https://github.com/StarsDust01/fluent-prayer-times/releases/latest", _s.Theme);
				_toast.ShowToast();
				if (text != "update")
				{
					_toast.PreviewState(text.Substring(7));
				}
			}
		}
		if (_pt == 1 && text.StartsWith("updates"))
		{
			Nav.SelectedItem = Nav.FooterMenuItems[text.Contains("about") ? 1 : 0];
			ShowFlyout(nearCursor: false);
			_updateState = (text.Contains("available") ? "available" : (text.Contains("error") ? "error" : (text.Contains("checking") ? "checking" : "current")));
			_checkingUpdates = _updateState == "checking";
			_manualUpdateVersion = "v9.0.0";
			_manualUpdateUrl = ((_updateState == "available") ? "https://github.com/StarsDust01/fluent-prayer-times/releases/latest" : null);
			_manualUpdateAsset = ((_updateState == "available") ? PreviewAsset : null);
			RefreshUpdateCards();
		}
		if (_pt == 3 && text.StartsWith("updates"))
		{
			(text.Contains("about") ? UpdateAboutCard : UpdateSettingsCard).StartBringIntoView();
		}
		if (_pt == 3 && text == "calculation")
		{
			CalculationCard.StartBringIntoView();
		}
		if (_pt == 3 && (text == "sounds" || text == "sounds-closed"))
		{
			PanelSounds.StartBringIntoView();
		}
		if (_pt == 4 && text == "sounds" && PanelSounds.Children.Count > 0 && PanelSounds.Children[0] is StackPanel stackPanel && stackPanel.Children.Count > 1 && stackPanel.Children[1] is Grid grid && grid.Children[0] is ComboBox comboBox)
		{
			comboBox.IsDropDownOpen = true;
		}
		if (_pt == 3 && text == "picker")
		{
			AccentCustom_Click(this, new RoutedEventArgs());
		}
		if (_pt == 2 && (text == "hover" || text == "hoverstress"))
		{
			RectInt32 workArea = DisplayArea.Primary.WorkArea;
			_hover.Pinned = true;
			_hover.Update(_hTitle, _hCount, _hUnit, _hSub, _hColor, _hMsg);
			_hover.ShowAt(workArea.X + workArea.Width / 2, workArea.Y + workArea.Height - 10, _s.Theme);
		}
		if (text == "main" && _pt == 4)
		{
			try
			{
				long num = ((IntPtr)GetWindowLongPtr(_hwnd, -20)).ToInt64();
				File.AppendAllText(Path.Combine(App.AppData, "preview.log"), $"main toolwindow={(num & 0x80) != 0} appwindow={(num & 0x40000) != 0} visible={base.AppWindow.IsVisible} opacity={RootGrid.Opacity:F2}\n");
			}
			catch
			{
			}
		}
		if (text == "hoverstress" && _pt > 3)
		{
			string path = Path.Combine(App.AppData, "hoverstress.log");
			if (_pt % 2 == 0)
			{
				_hover.Hide();
			}
			else
			{
				RectInt32 workArea2 = DisplayArea.Primary.WorkArea;
				_hover.Update(_hTitle, _hCount, _hUnit, _hSub, _hColor, _hMsg);
				_hover.ShowAt(workArea2.X + workArea2.Width / 2, workArea2.Y + workArea2.Height - 10, _s.Theme);
			}
			try
			{
				File.AppendAllText(path, _pt + " " + _hover.Diag() + "\n");
			}
			catch
			{
			}
		}
	}

	private void SetMainCount(string count, string unit, Color? color, bool msg)
	{
		TxtCountdown.Text = count;
		TxtUnit.Text = unit;
		TxtCountdown.Foreground = (color.HasValue ? new SolidColorBrush(color.Value) : _countBrush);
		TxtCountdown.FontSize = (msg ? 28 : 40);
	}

	private void SetHover(string title, string count, string unit, string sub, Color? color, bool msg)
	{
		_hTitle = title;
		_hCount = count;
		_hUnit = unit;
		_hSub = sub;
		_hColor = color;
		_hMsg = msg;
		if (_hover.Shown)
		{
			_hover.Update(title, count, unit, sub, color, msg);
		}
	}

	private void PushWidget(string n, string t, string c, string header, Color? color, bool msg, string unit = "")
	{
		_wName = n;
		_wTime = t;
		_wCount = c;
		_wUnit = unit;
		_wHeader = header;
		_wColor = color;
		_wMsg = msg;
		_widget?.SetInfo(n, t, c, header, color, msg, unit);
	}

	private void LoadSettingsUi()
	{
		FillLangCombo();
		FillLocationCombos();
		TglAdhan.IsOn = _s.NotifyAdhan;
		BuildIqamaAndSounds();
		LoadCalculationUi();
		SyncCitySelection();
		TxtLat.Text = _s.Location.Lat.ToString("F4", CultureInfo.InvariantCulture);
		TxtLon.Text = _s.Location.Lon.ToString("F4", CultureInfo.InvariantCulture);
		string backdrop = _s.Backdrop;
		RadioButton radioButton = ((backdrop == "mica") ? RbMica : ((!(backdrop == "acrylic")) ? RbMicaAlt : RbAcrylic));
		radioButton.IsChecked = true;
		backdrop = _s.Theme;
		radioButton = ((backdrop == "light") ? RbThemeLight : ((!(backdrop == "dark")) ? RbThemeSys : RbThemeDark));
		radioButton.IsChecked = true;
		backdrop = _s.Dst;
		radioButton = ((backdrop == "off") ? RbDstOff : ((!(backdrop == "on")) ? RbDstAuto : RbDstOn));
		radioButton.IsChecked = true;
		TglStartup.IsOn = _s.RunAtStartup;
		TglWidget.IsOn = _s.WidgetVisible;
		TglAutoUpdate.IsOn = _s.AutoUpdate;
		InitAccentUi();
	}

	private void FillLocationCombos()
	{
		bool loading = _loading;
		_loading = true;
		CmbCountry.Items.Clear();
		CmbCity.Items.Clear();
		CmbCountry.Items.Add(L.T("كل دول العالم"));
		Country[] all = Countries.All;
		foreach (Country country in all)
		{
			CmbCountry.Items.Add(L.CountryName(country.Cc, country.Ar));
		}
		PopulateCities(string.IsNullOrEmpty(_s.Location.Cc) ? "EG" : _s.Location.Cc);
		_loading = loading;
	}

	private void PopulateCities(string cc)
	{
		_cityCc = cc;
		_cities.Clear();
		if (cc == "EG")
		{
			_cities.AddRange(Cities.All);
		}
		else
		{
			Country[] all = Countries.All;
			foreach (Country country in all)
			{
				if (country.Cc == cc)
				{
					_cities.Add(new City(country.Capital, country.Capital, country.Lat, country.Lon));
					break;
				}
			}
		}
		CmbCity.Items.Clear();
		foreach (City city in _cities)
		{
			CmbCity.Items.Add(L.T(city.Ar));
		}
	}

	private void FillLangCombo()
	{
		bool loading = _loading;
		_loading = true;
		CmbLang.Items.Clear();
		CmbLang.Items.Add("Auto / تلقائي");
		string[] native = L.Native;
		foreach (string item in native)
		{
			CmbLang.Items.Add(item);
		}
		CmbLang.SelectedIndex = ((!string.IsNullOrEmpty(_s.Lang)) ? Math.Max(0, Array.IndexOf(L.Codes, _s.Lang) + 1) : 0);
		_loading = loading;
	}

	private void Lang_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_loading && CmbLang.SelectedIndex >= 0)
		{
			_s.Lang = ((CmbLang.SelectedIndex == 0) ? "" : L.Codes[CmbLang.SelectedIndex - 1]);
			_s.Save();
			ApplyLanguage();
		}
	}

	private void ApplyFlow()
	{
		FlowDirection flow = L.Flow;
		try
		{
			nint windowHandle = WindowNative.GetWindowHandle(this);
			long num = ((IntPtr)GetWindowLongPtr(windowHandle, -20)).ToInt64();
			num = (L.Rtl ? (num | 0x400000) : (num & -4194305));
			SetWindowLongPtr(windowHandle, -20, new IntPtr(num));
		}
		catch
		{
		}
		TitleStack.HorizontalAlignment = (L.Rtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
		TitleStack.Margin = (L.Rtl ? new Thickness(0.0, 0.0, 14.0, 0.0) : new Thickness(14.0, 0.0, 0.0, 0.0));
		TitleStack.FlowDirection = flow;
		FrameworkElement[] array = new FrameworkElement[9] { Nav, PanelTimes, PanelCalendar, PanelConvert, PanelAzkar, PanelSettings, PanelAbout, BtnAccentCustom, BtnAccentReset };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].FlowDirection = flow;
		}
	}

	public void ApplyLanguage()
	{
		L.Set(_s.Lang);
		ApplyFlow();
		L.Refresh();
		RefreshUpdateCards();
		base.Title = L.T("مواقيت الصلاة - Fluent Prayer Times");
		try
		{
			Version version = typeof(App).Assembly.GetName().Version;
			if (version != null)
			{
				TxtVersion.Text = L.T("الإصدار ") + version.Major + "." + version.Minor + "." + version.Build;
			}
		}
		catch
		{
		}
		bool loading = _loading;
		_loading = true;
		FillLocationCombos();
		SyncCitySelection();
		LoadCalculationUi();
		PanelCalendar.Children.Clear();
		PanelCalendar.Children.Add(new CalendarPage());
		PanelConvert.Children.Clear();
		PanelConvert.Children.Add(new ConverterPage());
		PanelAzkar.Children.Clear();
		PanelAzkar.Children.Add(new AzkarPage(_s, OnAzkarChanged));
		BuildIqamaAndSounds();
		_loading = loading;
		_widget?.ApplyLang();
		if (_res != null)
		{
			ShowDatesAndCity(_res);
		}
		_hl = -2;
		try
		{
			Tick();
		}
		catch
		{
		}
	}

	private void SyncCitySelection()
	{
		bool loading = _loading;
		_loading = true;
		string text = (string.IsNullOrEmpty(_s.Location.Cc) ? "EG" : _s.Location.Cc);
		if (text != _cityCc)
		{
			PopulateCities(text);
		}
		int num = _cities.FindIndex((City c) => c.Name == _s.Location.Name);
		CmbCity.SelectedIndex = num;
		int num2 = Array.FindIndex(Countries.All, (Country c) => c.Cc == _s.Location.Cc);
		CmbCountry.SelectedIndex = ((num2 >= 0) ? (num2 + 1) : 0);
		if (num < 0)
		{
			CmbCity.PlaceholderText = L.T(_s.Location.NameAr);
		}
		if (_s.LocationPending)
		{
			CmbCity.SelectedIndex = -1;
			CmbCountry.SelectedIndex = -1;
			CmbCity.PlaceholderText = L.T("اختر مدينتك");
		}
		TxtLat.Text = _s.Location.Lat.ToString("F4", CultureInfo.InvariantCulture);
		TxtLon.Text = _s.Location.Lon.ToString("F4", CultureInfo.InvariantCulture);
		_loading = loading;
	}

	private void LocationChanged()
	{
		_s.LocationPending = false;
		LoadCalculationUi();
		_s.Save();
		SyncCitySelection();
		ReloadAsync();
	}

	private void Country_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_loading && CmbCountry.SelectedIndex > 0)
		{
			Country country = Countries.All[CmbCountry.SelectedIndex - 1];
			if (country.Cc == _s.Location.Cc)
			{
				_ = _s.Location.UseCity;
			}
			if (country.Cc == "EG")
			{
				_s.Location = new Loc
				{
					Name = "Cairo",
					NameAr = "القاهرة",
					Lat = 30.0444,
					Lon = 31.2357,
					UseCity = true
				};
			}
			else
			{
				_s.Location = new Loc
				{
					Name = country.Capital,
					NameAr = country.Capital,
					Lat = country.Lat,
					Lon = country.Lon,
					UseCity = false,
					CountryAr = country.Ar,
					Cc = country.Cc
				};
			}
			LocationChanged();
		}
	}

	private void City_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_loading && CmbCity.SelectedIndex >= 0 && CmbCity.SelectedIndex < _cities.Count)
		{
			City city = _cities[CmbCity.SelectedIndex];
			string countryAr = ((_cityCc == "EG") ? "مصر" : (Array.Find(Countries.All, (Country k) => k.Cc == _cityCc)?.Ar ?? ""));
			_s.Location = new Loc
			{
				Name = city.Name,
				NameAr = city.Ar,
				Lat = city.Lat,
				Lon = city.Lon,
				UseCity = (_cityCc == "EG"),
				CountryAr = countryAr,
				Cc = _cityCc
			};
			LocationChanged();
		}
	}

	private void Search_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Enter)
		{
			Search_Click(sender, e);
		}
	}

	private async void Search_Click(object sender, RoutedEventArgs e)
	{
		string text = TxtSearch.Text.Trim();
		if (text.Length < 2)
		{
			return;
		}
		try
		{
			string cc = ((CmbCountry.SelectedIndex > 0) ? Countries.All[CmbCountry.SelectedIndex - 1].Cc : null);
			_places = await Times.Search(text, cc);
			_loading = true;
			LstPlaces.ItemsSource = _places.Select((Times.Place p) => p.Label).ToList();
			_loading = false;
			LstPlaces.Visibility = Visibility.Visible;
			if (_places.Count == 0)
			{
				TxtDetect.Text = L.T("لا توجد نتائج");
			}
		}
		catch
		{
			TxtDetect.Text = L.T("فشل البحث - تأكد من الاتصال بالإنترنت");
		}
	}

	private void Place_Selected(object sender, SelectionChangedEventArgs e)
	{
		if (!_loading && LstPlaces.SelectedIndex >= 0 && LstPlaces.SelectedIndex < _places.Count)
		{
			Times.Place place = _places[LstPlaces.SelectedIndex];
			_s.Location = new Loc
			{
				Name = place.Name,
				NameAr = place.Name,
				Lat = place.Lat,
				Lon = place.Lon,
				UseCity = false,
				CountryAr = place.Country,
				Cc = place.Cc
			};
			LstPlaces.Visibility = Visibility.Collapsed;
			LocationChanged();
		}
	}

	private async Task AutoLocateFirstRun()
	{
		try
		{
			string text;
			Times.Place place2;
			if (App.E2EDetect == "ok")
			{
				Times.Place place = new Times.Place("E2E City", 40.7128, -74.006, "E2E City", "United States", "US");
				text = "e2e";
				place2 = place;
			}
			else if (App.E2EDetect == "fail")
			{
				place2 = null;
				text = "";
				_ = ((Times.Place?)null, "");
			}
			else
			{
				(Times.Place, string) tuple = await Times.Detect();
				(place2, text) = tuple;
				_ = (tuple.Item1, tuple.Item2);
			}
			if (!_s.LocationPending)
			{
				return;
			}
			if (place2 == null)
			{
				_locateFailed = true;
				_loading = true;
				TxtDetect.Text = L.T("تعذ\u0651ر تحديد الموقع");
				_loading = false;
				ReloadAsync();
				{
					foreach (NavigationViewItem item in Nav.MenuItems.Concat(Nav.FooterMenuItems).OfType<NavigationViewItem>())
					{
						if (item.Tag as string == "settings")
						{
							Nav.SelectedItem = item;
							break;
						}
					}
					return;
				}
			}
			_s.Location = new Loc
			{
				Name = place2.Name,
				NameAr = place2.Name,
				Lat = place2.Lat,
				Lon = place2.Lon,
				UseCity = false,
				CountryAr = place2.Country,
				Cc = place2.Cc
			};
			_loading = true;
			TxtDetect.Text = L.T("تم: ") + text;
			_loading = false;
			LocationChanged();
		}
		catch
		{
		}
	}

	private async void Detect_Click(object sender, RoutedEventArgs e)
	{
		TxtDetect.Text = L.T("جار\u064d تحديد موقعك...");
		var (place, text) = await Times.Detect();
		if (place == null)
		{
			TxtDetect.Text = L.T("تعذ\u0651ر تحديد الموقع");
			return;
		}
		_s.Location = new Loc
		{
			Name = place.Name,
			NameAr = place.Name,
			Lat = place.Lat,
			Lon = place.Lon,
			UseCity = false,
			CountryAr = place.Country,
			Cc = place.Cc
		};
		TxtDetect.Text = L.T("تم: ") + text;
		LocationChanged();
	}

	private async void Coords_Click(object sender, RoutedEventArgs e)
	{
		if (double.TryParse(TxtLat.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var la) && double.TryParse(TxtLon.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lo) && la >= -90.0 && la <= 90.0 && lo >= -180.0 && lo <= 180.0)
		{
			(string, string, string)? tuple = await Times.Reverse(la, lo);
			string text = ((tuple.HasValue && tuple.Value.Item1 != "") ? tuple.Value.Item1 : (L.T("إحداثيات") + $" ({la:F3}, {lo:F3})"));
			_s.Location = new Loc
			{
				Name = text,
				NameAr = text,
				Lat = la,
				Lon = lo,
				UseCity = false,
				CountryAr = (tuple?.Item2 ?? ""),
				Cc = (tuple?.Item3 ?? "")
			};
			LocationChanged();
		}
		else
		{
			TxtDetect.Text = L.T("إحداثيات غير صحيحة");
		}
	}

	private void Dst_Checked(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			_s.Dst = (string)((RadioButton)sender).Tag;
			_s.Save();
			ReloadAsync();
		}
	}

	private void Backdrop_Checked(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			_s.Backdrop = (string)((RadioButton)sender).Tag;
			_s.Save();
			ApplyAppearance();
		}
	}

	private void Theme_Checked(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			_s.Theme = (string)((RadioButton)sender).Tag;
			_s.Save();
			ApplyAppearance();
		}
	}

	private void BuildIqamaAndSounds()
	{
		PanelIqama.Children.Clear();
		int[] array = new int[5] { 0, 2, 3, 4, 5 };
		foreach (int num in array)
		{
			int idx = num;
			Grid grid = new Grid
			{
				ColumnSpacing = 12.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			TextBlock textBlock = new TextBlock
			{
				Text = Times.Names[num],
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 14.0
			};
			NumberBox numberBox = new NumberBox
			{
				Minimum = 2.0,
				Maximum = 180.0,
				Value = _s.IqamaFor(num),
				SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
				SmallChange = 1.0,
				LargeChange = 5.0,
				Width = 140.0,
				FlowDirection = FlowDirection.LeftToRight,
				Header = null
			};
			numberBox.ValueChanged += delegate(NumberBox s2, NumberBoxValueChangedEventArgs a2)
			{
				if (!_loading)
				{
					double a3 = (double.IsNaN(a2.NewValue) ? ((double)_s.IqamaFor(idx)) : a2.NewValue);
					int[] array2 = ((_s.IqamaMin != null && _s.IqamaMin.Length >= 6) ? _s.IqamaMin : new int[6] { 20, 0, 20, 20, 15, 20 });
					array2[idx] = (int)Math.Clamp(Math.Round(a3), 2.0, 180.0);
					_s.IqamaMin = array2;
					_s.Save();
				}
			};
			Grid.SetColumn(textBlock, 0);
			Grid.SetColumn(numberBox, 1);
			grid.Children.Add(textBlock);
			grid.Children.Add(numberBox);
			PanelIqama.Children.Add(grid);
		}
		PanelSounds.Children.Clear();
		PanelSounds.Children.Add(SoundRow(L.T("صوت الأذان"), "adhan", new string[4]
		{
			L.T("الافتراضي (صوت الإشعار)"),
			L.T("أذان مختصر"),
			L.T("أذان كامل"),
			L.T("بدون صوت")
		}, _s.AdhanSound, delegate(string v)
		{
			_s.AdhanSound = v;
			_s.Save();
		}, _s.AdhanNotify, delegate(bool on)
		{
			_s.AdhanNotify = on;
			_s.Save();
		}));
		PanelSounds.Children.Add(SoundRow(L.T("صوت الإقامة"), "iqama", new string[4]
		{
			L.T("الافتراضي (صوت الإشعار)"),
			L.T("إقامة مختصرة"),
			L.T("إقامة كاملة"),
			L.T("بدون صوت")
		}, _s.IqamaSound, delegate(string v)
		{
			_s.IqamaSound = v;
			_s.Save();
		}, _s.IqamaNotify, delegate(bool on)
		{
			_s.IqamaNotify = on;
			_s.Save();
		}));
	}

	private UIElement SoundRow(string title, string kind, string[] labels, string cur, Action<string> save, bool on, Action<bool> saveOn)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 6.0
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		TextBlock textBlock = new TextBlock
		{
			Text = title,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		ToggleSwitch tgl = new ToggleSwitch
		{
			IsOn = on,
			OnContent = L.T("مفع\u0651ل"),
			OffContent = L.T("متوقف"),
			MinWidth = 0.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(textBlock, 0);
		Grid.SetColumn(tgl, 1);
		grid.Children.Add(textBlock);
		grid.Children.Add(tgl);
		stackPanel.Children.Add(grid);
		Grid grid2 = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		ComboBox cmb = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		foreach (string item in labels)
		{
			cmb.Items.Add(item);
		}
		cmb.SelectedIndex = Math.Max(0, Array.IndexOf(SoundTags, cur));
		Button test = new Button
		{
			Content = L.T("تجربة")
		};
		TextBlock note = HijriUtil.Sec(new TextBlock
		{
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap
		});
		cmb.SelectionChanged += delegate
		{
			if (!_loading && cmb.SelectedIndex >= 0)
			{
				save(SoundTags[cmb.SelectedIndex]);
				note.Text = "";
			}
		};
		test.Click += delegate
		{
			string text = SoundTags[Math.Max(0, cmb.SelectedIndex)];
			if (text == "default")
			{
				note.Text = L.T("الصوت الافتراضي هو صوت إشعار ويندوز.");
			}
			else if (text == "none")
			{
				note.Text = L.T("لن ي\u064fشغ\u064e\u0651ل أي صوت مع الإشعار.");
			}
			else
			{
				note.Text = (Sounds.Play(kind, text) ? "" : L.T("ملف الصوت غير موجود بعد في مجلد Audio."));
			}
		};
		tgl.Toggled += delegate
		{
			if (!_loading)
			{
				saveOn(tgl.IsOn);
				cmb.IsEnabled = tgl.IsOn;
				test.IsEnabled = tgl.IsOn;
			}
		};
		cmb.IsEnabled = on;
		test.IsEnabled = on;
		Grid.SetColumn(cmb, 0);
		Grid.SetColumn(test, 1);
		grid2.Children.Add(cmb);
		grid2.Children.Add(test);
		stackPanel.Children.Add(grid2);
		stackPanel.Children.Add(note);
		return stackPanel;
	}

	private static Version? ReleaseVersion(string? tag)
	{
		if (!Version.TryParse(tag?.TrimStart('v', 'V'), out Version result))
		{
			return null;
		}
		return result;
	}

	internal static string? UpdateDownload(JsonElement release, Version installed)
	{
		return UpdateAssetFor(release, installed)?.Url;
	}

	internal static UpdateAsset? UpdateAssetFor(JsonElement release, Version installed)
	{
		if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
		{
			return null;
		}
		Version version = ReleaseVersion(release.GetProperty("tag_name").GetString());
		if (version == null || version <= installed)
		{
			return null;
		}
		string[] array = new string[2] { "installer-.NET-runtime-dependent-win-x64.exe", "installer-win-x64.exe" };
		foreach (string text in array)
		{
			foreach (JsonElement item in release.GetProperty("assets").EnumerateArray())
			{
				string text2 = item.GetProperty("name").GetString();
				if (text2 != "FluentPrayerTimes-v" + version.ToString(3) + "-" + text)
				{
					continue;
				}
				string text3 = item.GetProperty("browser_download_url").GetString();
				if (Uri.TryCreate(text3, UriKind.Absolute, out Uri result) && result.Scheme == "https" && result.Host == "github.com" && result.AbsolutePath.StartsWith("/StarsDust01/fluent-prayer-times/releases/download/", StringComparison.OrdinalIgnoreCase))
				{
					JsonElement value;
					long size = ((item.TryGetProperty("size", out value) && value.ValueKind == JsonValueKind.Number) ? value.GetInt64() : 0);
					string sha = null;
					if (item.TryGetProperty("digest", out var value2) && value2.ValueKind == JsonValueKind.String && (value2.GetString() ?? "").StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
					{
						sha = value2.GetString().Substring(7);
					}
					return new UpdateAsset(text3, text2, size, sha);
				}
			}
		}
		throw new InvalidOperationException("New release has no supported installer.");
	}

	private void Update_Click(object sender, RoutedEventArgs e)
	{
		CheckForUpdatesAsync();
	}

	private async Task LiveUpdatePreviewAsync()
	{
		try
		{
			using HttpClient http = new HttpClient();
			http.DefaultRequestHeaders.UserAgent.ParseAdd("FluentPrayerTimes-ci");
			using JsonDocument jsonDocument = JsonDocument.Parse(await http.GetStringAsync("https://api.github.com/repos/StarsDust01/fluent-prayer-times/releases/latest"));
			UpdateAsset updateAsset = UpdateAssetFor(jsonDocument.RootElement, new Version(1, 3, 3));
			if (updateAsset == null)
			{
				return;
			}
			_toast = new UpdateToast("1.3.4", updateAsset, "https://github.com/StarsDust01/fluent-prayer-times/releases/latest", _s.Theme);
			_toast.ShowToast();
			_toast.Begin();
		}
		catch
		{
		}
	}

	private void UpdateInstall_Click(object sender, RoutedEventArgs e)
	{
		if (!(_manualUpdateAsset == null))
		{
			try
			{
				_toast?.Close();
			}
			catch
			{
			}
			_toast = new UpdateToast((_manualUpdateVersion ?? "").TrimStart('v', 'V'), _manualUpdateAsset, "https://github.com/StarsDust01/fluent-prayer-times/releases/latest", _s.Theme);
			_toast.ShowToast();
			_toast.Begin();
		}
	}

	private void RefreshUpdateCards()
	{
		string text = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
		TextBlock txtUpdateVersion = TxtUpdateVersion;
		string text2 = (TxtAboutUpdateVersion.Text = L.T("الإصدار الحالي: ") + "v" + text);
		txtUpdateVersion.Text = text2;
		string text4 = _updateState switch
		{
			"checking" => L.T("جار\u064d التحقق..."), 
			"available" => L.T("يتوفر الإصدار ") + _manualUpdateVersion + ".", 
			"current" => L.T("أنت على أحدث إصدار") + " (v" + text + ").", 
			"error" => L.T("تعذر التحقق من التحديثات. تحقق من اتصالك بالإنترنت وحاول مرة أخرى."), 
			_ => "", 
		};
		TextBlock txtUpdate = TxtUpdate;
		string text5 = (TxtAboutUpdate.Text = text4);
		txtUpdate.Text = text5;
		Button[] array = new Button[2] { BtnUpdate, BtnAboutUpdate };
		foreach (Button obj in array)
		{
			obj.IsEnabled = !_checkingUpdates;
			obj.Content = L.T("التحقق من التحديثات");
			obj.HorizontalAlignment = HorizontalAlignment.Left;
		}
		array = new Button[2] { BtnUpdateInstall, BtnAboutUpdateInstall };
		foreach (Button obj2 in array)
		{
			obj2.Content = L.T("تنزيل وتثبيت");
			obj2.HorizontalAlignment = HorizontalAlignment.Left;
			obj2.Visibility = ((_manualUpdateAsset == null) ? Visibility.Collapsed : Visibility.Visible);
		}
		HyperlinkButton[] array2 = new HyperlinkButton[2] { UpdateLink, AboutUpdateLink };
		foreach (HyperlinkButton obj3 in array2)
		{
			obj3.Content = L.T("فتح صفحة التنزيل");
			obj3.HorizontalAlignment = HorizontalAlignment.Left;
			obj3.NavigateUri = ((_manualUpdateUrl == null) ? null : new Uri(_manualUpdateUrl));
			obj3.Visibility = ((_manualUpdateUrl == null) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private async Task CheckForUpdatesAsync()
	{
		if (_checkingUpdates)
		{
			return;
		}
		_checkingUpdates = true;
		_updateState = "checking";
		_manualUpdateUrl = null;
		_manualUpdateAsset = null;
		RefreshUpdateCards();
		try
		{
			using HttpClient http = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20.0)
			};
			http.DefaultRequestHeaders.UserAgent.ParseAdd("FluentPrayerTimes/" + (typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"));
			http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
			using HttpResponseMessage response = await http.GetAsync("https://api.github.com/repos/StarsDust01/fluent-prayer-times/releases/latest");
			response.EnsureSuccessStatusCode();
			using JsonDocument jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
			Version installed = ReleaseVersion(typeof(MainWindow).Assembly.GetName().Version?.ToString(3)) ?? new Version(0, 0, 0);
			_manualUpdateAsset = UpdateAssetFor(jsonDocument.RootElement, installed);
			_manualUpdateUrl = _manualUpdateAsset?.Url;
			_manualUpdateVersion = jsonDocument.RootElement.GetProperty("tag_name").GetString();
			_updateState = ((_manualUpdateUrl == null) ? "current" : "available");
		}
		catch (Exception ex)
		{
			try
			{
				Directory.CreateDirectory(App.AppData);
				File.AppendAllText(Path.Combine(App.AppData, "update.log"), DateTime.Now.ToString() + " " + ex.Message + "\n");
			}
			catch
			{
			}
			_updateState = "error";
		}
		finally
		{
			_checkingUpdates = false;
			if (!_quit)
			{
				RefreshUpdateCards();
			}
		}
	}

	private void Startup_Toggled(object sender, RoutedEventArgs e)
	{
		if (_loading)
		{
			return;
		}
		_s.RunAtStartup = TglStartup.IsOn;
		_s.Save();
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (_s.RunAtStartup)
			{
				registryKey?.SetValue("FluentPrayerTimes", "\"" + Environment.ProcessPath + "\" --tray");
			}
			else
			{
				registryKey?.DeleteValue("FluentPrayerTimes", throwOnMissingValue: false);
			}
		}
		catch
		{
		}
	}
}
