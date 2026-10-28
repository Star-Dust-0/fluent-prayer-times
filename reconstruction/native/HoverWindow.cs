using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using WinRT.Interop;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace FluentPrayerTimes;

internal sealed class HoverWindow : Window
{
	private struct CurPt
	{
		public int X;

		public int Y;
	}

	private readonly nint _hwnd;

	private readonly Grid _root = new Grid();

	private readonly TextBlock _title = new TextBlock
	{
		FontSize = 20.0,
		FontWeight = FontWeights.SemiBold,
		TextTrimming = TextTrimming.CharacterEllipsis
	};

	private readonly TextBlock _unit = HijriUtil.Sec(new TextBlock
	{
		FontSize = 18.0,
		VerticalAlignment = VerticalAlignment.Bottom,
		Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
	});

	private readonly TextBlock _count = HijriUtil.Themed(new TextBlock
	{
		FontSize = 42.0,
		FontWeight = FontWeights.SemiBold,
		FlowDirection = FlowDirection.LeftToRight
	}, "AccentTextFillColorPrimaryBrush");

	private readonly TextBlock _sub = HijriUtil.Sec(new TextBlock
	{
		FontSize = 15.0,
		TextTrimming = TextTrimming.CharacterEllipsis
	});

	private readonly DispatcherQueueTimer _timer;

	private readonly Func<bool> _overTray;

	private int _miss;

	private readonly DispatcherQueueTimer _delay;

	private long _entered;

	private int _hoverDelay;

	private Action? _pending;

	private bool _below;

	private RectInt32 _rect;

	private int _gen;

	private Storyboard? _motion;

	private readonly DispatcherQueueTimer _heal;

	private int _healTries;

	private readonly Brush _accent;

	public bool Pinned;

	public bool Shown { get; private set; }

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint h);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out CurPt p);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint h, int i);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint h, int i, nint v);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint h, int cmd);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint h, nint after, int x, int y, int cx, int cy, uint flags);

	private void Raise()
	{
		try
		{
			SetWindowPos(_hwnd, new IntPtr(-1), 0, 0, 0, 0, 19u);
		}
		catch
		{
		}
	}

	public HoverWindow(Func<bool> overTray)
	{
		_overTray = overTray;
		base.Title = "Fluent Prayer Times Hover";
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
			base.SystemBackdrop = new ActiveAcrylic();
		}
		catch
		{
		}
		base.AppWindow.Closing += delegate(AppWindow a, AppWindowClosingEventArgs e)
		{
			e.Cancel = true;
			Hide();
		};
		long num = GetWindowLongPtr(_hwnd, -20);
		SetWindowLongPtr(_hwnd, -20, (nint)(num | 0x80 | 0x8000000 | 8));
		WindowChrome.Apply(_hwnd);
		Border item = HijriUtil.Themed("SolidBackgroundFillColorBaseBrush", 0.35);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			Padding = new Thickness(20.0, 14.0, 20.0, 14.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			FlowDirection = FlowDirection.LeftToRight,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		stackPanel2.Children.Add(_unit);
		stackPanel2.Children.Add(_count);
		stackPanel.Children.Add(_title);
		stackPanel.Children.Add(stackPanel2);
		stackPanel.Children.Add(_sub);
		_root.Children.Add(item);
		_root.Children.Add(stackPanel);
		_root.FlowDirection = L.Flow;
		_root.RenderTransform = new TranslateTransform();
		_accent = _count.Foreground;
		base.Content = _root;
		_heal = base.DispatcherQueue.CreateTimer();
		_heal.Interval = TimeSpan.FromMilliseconds(90.0);
		_heal.Tick += delegate
		{
			Heal();
		};
		_timer = base.DispatcherQueue.CreateTimer();
		_timer.Interval = TimeSpan.FromMilliseconds(200.0);
		_timer.Tick += delegate
		{
			Watch();
		};
		_delay = base.DispatcherQueue.CreateTimer();
		_delay.Interval = TimeSpan.FromMilliseconds(50.0);
		_delay.Tick += delegate
		{
			if (!_overTray())
			{
				CancelPending();
			}
			else if (Environment.TickCount64 - _entered >= _hoverDelay)
			{
				Action? pending = _pending;
				CancelPending();
				pending?.Invoke();
			}
		};
	}

	public void RefreshTheme()
	{
		ElementTheme requestedTheme = _root.RequestedTheme;
		_root.RequestedTheme = ((requestedTheme == ElementTheme.Dark) ? ElementTheme.Light : ElementTheme.Dark);
		_root.RequestedTheme = requestedTheme;
	}

	public void RequestShow(Action show)
	{
		if (!Shown && !_delay.IsRunning)
		{
			_pending = show;
			_hoverDelay = NativeMotion.HoverDelay();
			_entered = Environment.TickCount64;
			_delay.Start();
		}
	}

	private void CancelPending()
	{
		_delay.Stop();
		_pending = null;
	}

	public void Update(string title, string count, string unit, string sub, Color? color = null, bool message = false)
	{
		_title.Text = title;
		_count.Text = count;
		_unit.Text = unit;
		_sub.Text = sub;
		_count.Foreground = (color.HasValue ? new SolidColorBrush(color.Value) : _accent);
		_count.FontSize = (message ? 28 : 42);
	}

	public string Diag()
	{
		return $"shown={Shown} size={base.AppWindow.Size.Width}x{base.AppWindow.Size.Height} want={_rect.Width}x{_rect.Height} actual={_root.ActualWidth:F0}x{_root.ActualHeight:F0} opacity={_root.Opacity:F2}";
	}

	private void Heal()
	{
		if (!Shown)
		{
			_heal.Stop();
			return;
		}
		Raise();
		try
		{
			SizeInt32 size = base.AppWindow.Size;
			int num;
			if (Math.Abs(size.Width - _rect.Width) <= 2 && Math.Abs(size.Height - _rect.Height) <= 2 && !(_root.ActualWidth < 50.0))
			{
				num = ((_root.ActualHeight < 50.0) ? 1 : 0);
				if (num == 0)
				{
					goto IL_00c0;
				}
			}
			else
			{
				num = 1;
			}
			base.AppWindow.MoveAndResize(_rect);
			_root.InvalidateMeasure();
			_root.UpdateLayout();
			ShowWindow(_hwnd, 4);
			goto IL_00c0;
			IL_00c0:
			if (num == 0 || ++_healTries > 8)
			{
				_heal.Stop();
			}
		}
		catch
		{
			_heal.Stop();
		}
	}

	private bool CursorInside()
	{
		if (!GetCursorPos(out var p))
		{
			return false;
		}
		PointInt32 position = base.AppWindow.Position;
		SizeInt32 size = base.AppWindow.Size;
		if (p.X >= position.X - 4 && p.X <= position.X + size.Width + 4 && p.Y >= position.Y - 4)
		{
			return p.Y <= position.Y + size.Height + 4;
		}
		return false;
	}

	private void Watch()
	{
		if (!Shown)
		{
			_timer.Stop();
		}
		else if (!Pinned)
		{
			if (_overTray() || CursorInside())
			{
				_miss = 0;
			}
			else if (++_miss >= 3)
			{
				Hide();
			}
		}
	}

	public void ShowAt(int ax, int ay, string theme)
	{
		_miss = 0;
		if (!Shown)
		{
			RectInt32 workArea = DisplayArea.GetFromPoint(new PointInt32(ax, ay), DisplayAreaFallback.Nearest).WorkArea;
			Grid root = _root;
			ElementTheme requestedTheme = ((theme == "light") ? ElementTheme.Light : ((theme == "dark") ? ElementTheme.Dark : ElementTheme.Default));
			root.RequestedTheme = requestedTheme;
			double num = (double)GetDpiForWindow(_hwnd) / 96.0;
			if (num < 1.0)
			{
				num = 1.0;
			}
			int num2 = (int)(340.0 * num);
			int num3 = (int)(160.0 * num);
			int x = Math.Clamp(ax - num2 / 2, workArea.X + 8, workArea.X + workArea.Width - num2 - 8);
			int num4 = ((ay > workArea.Y + workArea.Height / 2) ? (workArea.Y + workArea.Height - num3 - 8) : (workArea.Y + 8));
			_rect = new RectInt32(x, num4, num2, num3);
			_below = num4 > ay;
			_gen++;
			_motion?.Stop();
			_root.Opacity = 0.0;
			base.AppWindow.MoveAndResize(_rect);
			ShowWindow(_hwnd, 4);
			base.AppWindow.MoveAndResize(_rect);
			WindowChrome.Apply(_hwnd);
			Raise();
			_root.InvalidateMeasure();
			_root.UpdateLayout();
			Shown = true;
			_timer.Start();
			_healTries = 0;
			_heal.Start();
			Animate(show: true, _gen);
		}
	}

	private void Animate(bool show, int gen, Action? done = null)
	{
		_motion?.Stop();
		if (!new UISettings().AnimationsEnabled || !NativeMotion.TooltipAnimations())
		{
			_root.Opacity = (show ? 1 : 0);
			((TranslateTransform)_root.RenderTransform).Y = 0.0;
			done?.Invoke();
			return;
		}
		TranslateTransform translateTransform = new TranslateTransform();
		_root.RenderTransform = translateTransform;
		double value = (show ? 160 : 120);
		double num = (_below ? (-10) : 10);
		CubicEase easingFunction = new CubicEase
		{
			EasingMode = ((!show) ? EasingMode.EaseIn : EasingMode.EaseOut)
		};
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			From = ((!show) ? 1 : 0),
			To = (show ? 1 : 0),
			Duration = new Duration(TimeSpan.FromMilliseconds(value)),
			EasingFunction = easingFunction
		};
		DoubleAnimation doubleAnimation2 = new DoubleAnimation
		{
			From = (show ? num : 0.0),
			To = (show ? 0.0 : num),
			Duration = new Duration(TimeSpan.FromMilliseconds(value)),
			EasingFunction = easingFunction,
			EnableDependentAnimation = true
		};
		Storyboard.SetTarget(doubleAnimation, _root);
		Storyboard.SetTargetProperty(doubleAnimation, "Opacity");
		Storyboard.SetTarget(doubleAnimation2, translateTransform);
		Storyboard.SetTargetProperty(doubleAnimation2, "Y");
		Storyboard storyboard = new Storyboard();
		storyboard.Children.Add(doubleAnimation);
		storyboard.Children.Add(doubleAnimation2);
		_motion = storyboard;
		storyboard.Completed += delegate
		{
			if (gen == _gen)
			{
				_root.Opacity = (show ? 1 : 0);
				done?.Invoke();
			}
		};
		storyboard.Begin();
	}

	public void Hide()
	{
		CancelPending();
		_timer.Stop();
		_heal.Stop();
		if (!Shown)
		{
			return;
		}
		Shown = false;
		int g = ++_gen;
		Animate(show: false, g, delegate
		{
			if (g == _gen && !Shown)
			{
				ShowWindow(_hwnd, 0);
			}
		});
	}
}
