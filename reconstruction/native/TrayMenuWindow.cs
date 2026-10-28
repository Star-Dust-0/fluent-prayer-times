using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Windows.Graphics;
using Windows.UI;

namespace FluentPrayerTimes;

internal sealed class TrayMenuWindow : Window
{
	private readonly Border _root = new Border();

	private readonly StackPanel _list = new StackPanel
	{
		Spacing = 2.0
	};

	private readonly nint _hwnd;

	private readonly Action<int> _onCmd;

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint h);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint h);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint h, int i);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint h, int i, nint v);

	public TrayMenuWindow(Action<int> onCmd)
	{
		_onCmd = onCmd;
		_root.Padding = new Thickness(4.0);
		_root.Child = _list;
		base.Content = _root;
		base.Title = "Fluent Prayer Times Menu";
		_hwnd = WindowNative.GetWindowHandle(this);
		if (base.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
			overlappedPresenter.IsResizable = false;
			overlappedPresenter.IsMaximizable = false;
			overlappedPresenter.IsMinimizable = false;
			overlappedPresenter.IsAlwaysOnTop = true;
		}
		try
		{
			base.AppWindow.IsShownInSwitchers = false;
		}
		catch
		{
		}
		try
		{
			base.SystemBackdrop = new DesktopAcrylicBackdrop();
		}
		catch
		{
		}
		base.AppWindow.Closing += delegate(AppWindow a, AppWindowClosingEventArgs e)
		{
			e.Cancel = true;
			a.Hide();
		};
		base.Activated += delegate(object s, WindowActivatedEventArgs e)
		{
			if (e.WindowActivationState == WindowActivationState.Deactivated)
			{
				base.AppWindow.Hide();
			}
		};
		long num = GetWindowLongPtr(_hwnd, -20);
		SetWindowLongPtr(_hwnd, -20, (nint)(num | 0x80));
		WindowChrome.Apply(_hwnd);
	}

	private void AddItem(string text, string glyph, int cmd, bool check = false)
	{
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		FontIcon item = new FontIcon
		{
			Glyph = glyph,
			FontSize = 16.0
		};
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontSize = 14.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(textBlock, 1);
		grid.Children.Add(item);
		grid.Children.Add(textBlock);
		if (check)
		{
			FontIcon fontIcon = new FontIcon
			{
				Glyph = "\ue73e",
				FontSize = 14.0
			};
			Grid.SetColumn(fontIcon, 2);
			grid.Children.Add(fontIcon);
		}
		Button button = new Button
		{
			Content = grid,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			Height = 36.0,
			CornerRadius = new CornerRadius(4.0)
		};
		button.Click += delegate
		{
			base.AppWindow.Hide();
			_onCmd(cmd);
		};
		_list.Children.Add(button);
	}

	public void ShowAt(int ax, int ay, string theme, bool widgetOn)
	{
		RectInt32 workArea = DisplayArea.GetFromPoint(new PointInt32(ax, ay), DisplayAreaFallback.Nearest).WorkArea;
		Border root = _root;
		ElementTheme requestedTheme = ((theme == "light") ? ElementTheme.Light : ((theme == "dark") ? ElementTheme.Dark : ElementTheme.Default));
		root.RequestedTheme = requestedTheme;
		_root.FlowDirection = L.Flow;
		_list.Children.Clear();
		AddItem(L.T("فتح"), "\ue8a7", 1);
		AddItem(L.T("ويدجت سطح المكتب"), "\ue8a1", 4, widgetOn);
		AddItem(L.T("الإعدادات"), "\ue713", 2);
		AddItem(L.T("التحقق من التحديثات"), "\ue895", 5);
		_list.Children.Add(new Border
		{
			Height = 1.0,
			Margin = new Thickness(4.0, 3.0, 4.0, 3.0),
			Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128))
		});
		AddItem(L.T("خروج"), "\ue7e8", 3);
		double num = (double)GetDpiForWindow(_hwnd) / 96.0;
		if (num < 1.0)
		{
			num = 1.0;
		}
		int num2 = (int)(230.0 * num);
		int num3 = (int)(203.0 * num);
		int x = Math.Clamp(ax - num2 / 2, workArea.X + 8, workArea.X + workArea.Width - num2 - 8);
		int y = ((ay > workArea.Y + workArea.Height / 2) ? (workArea.Y + workArea.Height - num3 - 8) : (workArea.Y + 8));
		base.AppWindow.MoveAndResize(new RectInt32(x, y, num2, num3));
		base.AppWindow.Show();
		Activate();
		SetForegroundWindow(_hwnd);
	}
}
