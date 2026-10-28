using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinRT;
using WinRT.Interop;
using Windows.Graphics;
using Windows.UI;

namespace FluentPrayerTimes;

public sealed partial class WidgetWindow : Window
{
	private struct CurPt
	{
		public int X;

		public int Y;
	}

	private static readonly (double fx, double fy)[] Anchors = new(double, double)[8]
	{
		(0.0, 0.0),
		(0.5, 0.0),
		(1.0, 0.0),
		(0.0, 0.5),
		(1.0, 0.5),
		(0.0, 1.0),
		(0.5, 1.0),
		(1.0, 1.0)
	};

	private readonly nint _hwnd;

	private bool _wantVisible;

	private DispatcherQueueTimer? _keep;

	private readonly Settings _s;

	private bool _drag;

	private CurPt _start;

	private PointInt32 _winStart;

	public Action? HiddenByUser;

	private Brush? _countBrush;

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint h);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out CurPt p);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint h, int attr, ref int val, int size);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint h);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint h);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint h, int cmd);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint h, nint after, int x, int y, int cx, int cy, uint flags);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint h, int i);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint h, int i, nint v);

	public void RefreshTheme()
	{
		ElementTheme requestedTheme = Surface.RequestedTheme;
		Surface.RequestedTheme = ((requestedTheme == ElementTheme.Dark) ? ElementTheme.Light : ElementTheme.Dark);
		Surface.RequestedTheme = requestedTheme;
	}

	public WidgetWindow(Settings s)
	{
		_s = s;
		InitializeComponent();
        Surface.PointerPressed += Surface_PointerPressed;
        Surface.PointerMoved += Surface_PointerMoved;
        Surface.PointerReleased += Surface_PointerReleased;
        BtnPin.Click += Pin_Click;
        BtnHide.Click += Close_Click;
		L.Register(base.Content);
		ApplyLang();
		base.Title = "Fluent Prayer Times Widget";
		base.SystemBackdrop = new DesktopAcrylicBackdrop();
		nint num = (_hwnd = WindowNative.GetWindowHandle(this));
		double num2 = (double)GetDpiForWindow(num) / 96.0;
		if (num2 < 1.0)
		{
			num2 = 1.0;
		}
		int width = (int)(180.0 * num2);
		int height = (int)(110.0 * num2);
		if (base.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
			overlappedPresenter.IsResizable = false;
			overlappedPresenter.IsMaximizable = false;
			overlappedPresenter.IsMinimizable = false;
			overlappedPresenter.IsAlwaysOnTop = _s.WidgetPinned;
		}
		try
		{
			base.AppWindow.IsShownInSwitchers = false;
		}
		catch
		{
		}
		WindowChrome.Apply(num);
		BtnPin.IsChecked = _s.WidgetPinned;
		base.AppWindow.Closing += delegate(AppWindow a, AppWindowClosingEventArgs e)
		{
			e.Cancel = true;
			a.Hide();
		};
		long num3 = GetWindowLongPtr(num, -20);
		SetWindowLongPtr(num, -20, (nint)(num3 | 0x80 | 0x8000000));
		base.AppWindow.Resize(new SizeInt32(width, height));
		ApplySnap(Math.Clamp(_s.WidgetSnap, 0, 7));
		_keep = base.DispatcherQueue.CreateTimer();
		_keep.Interval = TimeSpan.FromMilliseconds(400.0);
		_keep.Tick += delegate
		{
			KeepAlive();
		};
		_keep.Start();
	}

	private void KeepAlive()
	{
		if (!_wantVisible)
		{
			return;
		}
		try
		{
			if (IsIconic(_hwnd) || !IsWindowVisible(_hwnd))
			{
				ShowWindow(_hwnd, 4);
				WindowChrome.Apply(_hwnd);
			}
			if (_s.WidgetPinned)
			{
				SetWindowPos(_hwnd, new IntPtr(-1), 0, 0, 0, 0, 19u);
			}
		}
		catch
		{
		}
	}

	private void ApplySnap(int idx)
	{
		try
		{
			PointInt32 position = base.AppWindow.Position;
			SizeInt32 size = base.AppWindow.Size;
			RectInt32 workArea = DisplayArea.GetFromRect(new RectInt32(position.X, position.Y, size.Width, size.Height), DisplayAreaFallback.Nearest).WorkArea;
			double num = Math.Max(1.0, (double)GetDpiForWindow(_hwnd) / 96.0);
			int num2 = (int)(12.0 * num);
			(double fx, double fy) tuple = Anchors[idx];
			double item = tuple.fx;
			double item2 = tuple.fy;
			int x = workArea.X + num2 + (int)(item * (double)(workArea.Width - size.Width - 2 * num2));
			int y = workArea.Y + num2 + (int)(item2 * (double)(workArea.Height - size.Height - 2 * num2));
			base.AppWindow.Move(new PointInt32(x, y));
		}
		catch
		{
		}
	}

	private void SnapNearest()
	{
		PointInt32 position = base.AppWindow.Position;
		SizeInt32 size = base.AppWindow.Size;
		RectInt32 workArea = DisplayArea.GetFromRect(new RectInt32(position.X, position.Y, size.Width, size.Height), DisplayAreaFallback.Nearest).WorkArea;
		double num = ((double)position.X + (double)size.Width / 2.0 - (double)workArea.X) / (double)workArea.Width;
		double num2 = ((double)position.Y + (double)size.Height / 2.0 - (double)workArea.Y) / (double)workArea.Height;
		int num3 = 2;
		double num4 = double.MaxValue;
		for (int i = 0; i < Anchors.Length; i++)
		{
			double num5 = Math.Pow(num - (0.08 + 0.84 * Anchors[i].fx), 2.0) + Math.Pow(num2 - (0.12 + 0.76 * Anchors[i].fy), 2.0);
			if (num5 < num4)
			{
				num4 = num5;
				num3 = i;
			}
		}
		_s.WidgetSnap = num3;
		_s.Save();
		ApplySnap(num3);
	}

	public void ApplyLang()
	{
		TxtHeader.FlowDirection = L.Flow;
		PanelInfo.FlowDirection = L.Flow;
		TxtHeader.HorizontalAlignment = (L.Rtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
		TxtHeader.TextAlignment = ((!L.Rtl) ? TextAlignment.Left : TextAlignment.Right);
		PrayerRow.HorizontalAlignment = HorizontalAlignment.Center;
		ToolTipService.SetToolTip(BtnPin, L.T("تثبيت فوق كل النوافذ"));
		ToolTipService.SetToolTip(BtnHide, L.T("إخفاء"));
		L.Refresh();
	}

	public void SetInfo(string name, string time, string count, string header, Color? color, bool message, string unit = "")
	{
		if ((object)_countBrush == null)
		{
			_countBrush = TxtCount.Foreground;
		}
		TxtName.Text = name;
		TxtTime.Text = time;
		TxtCount.Text = count;
		TxtHeader.Text = header;
		TxtCount.Foreground = (color.HasValue ? new SolidColorBrush(color.Value) : _countBrush);
		TxtCount.FontSize = (message ? 15 : 32);
		TxtUnit.Text = unit;
		TxtUnit.Visibility = (string.IsNullOrEmpty(unit) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void ShowWidget()
	{
		_wantVisible = true;
		ApplySnap(Math.Clamp(_s.WidgetSnap, 0, 7));
		base.AppWindow.Show();
		WindowChrome.Apply(_hwnd);
	}

	public void HideWidget()
	{
		_wantVisible = false;
		base.AppWindow.Hide();
	}

	private void Surface_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (GetCursorPos(out _start))
		{
			_winStart = base.AppWindow.Position;
			_drag = true;
			Surface.CapturePointer(e.Pointer);
		}
	}

	private void Surface_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_drag && GetCursorPos(out var p))
		{
			base.AppWindow.Move(new PointInt32(_winStart.X + (p.X - _start.X), _winStart.Y + (p.Y - _start.Y)));
		}
	}

	private void Surface_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (_drag)
		{
			_drag = false;
			Surface.ReleasePointerCapture(e.Pointer);
			SnapNearest();
		}
	}

	private void Pin_Click(object sender, RoutedEventArgs e)
	{
		_s.WidgetPinned = BtnPin.IsChecked == true;
		if (base.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.IsAlwaysOnTop = _s.WidgetPinned;
		}
		_s.Save();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		HideWidget();
		HiddenByUser?.Invoke();
	}
}
