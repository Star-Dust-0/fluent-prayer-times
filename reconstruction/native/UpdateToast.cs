using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Windows.Graphics;

namespace FluentPrayerTimes;

internal sealed class UpdateToast : Window
{
	private readonly nint _hwnd;

	private readonly Grid _root = new Grid();

	private readonly string _version;

	private readonly string _fallback;

	private readonly UpdateAsset _asset;

	private readonly TextBlock _title = new TextBlock
	{
		FontSize = 16.0,
		FontWeight = FontWeights.SemiBold,
		TextWrapping = TextWrapping.Wrap
	};

	private readonly TextBlock _sub = HijriUtil.Sec(new TextBlock
	{
		FontSize = 13.0,
		TextWrapping = TextWrapping.Wrap
	});

	private readonly ProgressBar _bar = new ProgressBar
	{
		Minimum = 0.0,
		Maximum = 100.0,
		Visibility = Visibility.Collapsed
	};

	private readonly StackPanel _btns = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Spacing = 8.0
	};

	private CancellationTokenSource? _cts;

	private string? _file;

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint h, int cmd);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint h);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint h, nint after, int x, int y, int cx, int cy, uint flags);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint h, int i);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint h, int i, nint v);

	public UpdateToast(string version, UpdateAsset asset, string fallbackUrl, string theme)
	{
		_version = version;
		_asset = asset;
		_fallback = fallbackUrl;
		base.Title = "Fluent Prayer Times Update";
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
		long num = GetWindowLongPtr(_hwnd, -20);
		SetWindowLongPtr(_hwnd, -20, (nint)(num | 0x80 | 0x8000000 | 8));
		WindowChrome.Apply(_hwnd);
		Border item = HijriUtil.Themed("SolidBackgroundFillColorBaseBrush", 0.35);
		Border border = new Border
		{
			Width = 40.0,
			Height = 40.0,
			CornerRadius = new CornerRadius(20.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		border.Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
		border.Child = new FontIcon
		{
			Glyph = "\ue896",
			FontSize = 18.0,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			MaxWidth = 260.0
		};
		stackPanel.Children.Add(_title);
		stackPanel.Children.Add(_sub);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12.0
		};
		stackPanel2.Children.Add(border);
		stackPanel2.Children.Add(stackPanel);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 12.0,
			Padding = new Thickness(18.0, 16.0, 18.0, 16.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel3.Children.Add(stackPanel2);
		stackPanel3.Children.Add(_bar);
		stackPanel3.Children.Add(_btns);
		_root.Children.Add(item);
		_root.Children.Add(stackPanel3);
		_root.FlowDirection = L.Flow;
		Grid root = _root;
		root.RequestedTheme = ((theme == "light") ? ElementTheme.Light : ((theme == "dark") ? ElementTheme.Dark : ElementTheme.Default));
		base.Content = _root;
		base.Closed += delegate
		{
			try
			{
				_cts?.Cancel();
			}
			catch
			{
			}
		};
		SetState("offer");
	}

	private Button AddButton(string text, Action click, bool accent = false)
	{
		Button button = new Button
		{
			Content = text,
			MinWidth = 96.0
		};
		if (accent)
		{
			button.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
		}
		button.Click += delegate
		{
			click();
		};
		_btns.Children.Add(button);
		return button;
	}

	private void SetState(string state, string? detail = null)
	{
		_btns.Children.Clear();
		_bar.Visibility = ((!(state == "downloading")) ? Visibility.Collapsed : Visibility.Visible);
		switch (state)
		{
		case "downloading":
			_title.Text = L.T("جار\u064d تنزيل التحديث");
			_sub.Text = "v" + _version + "  0%";
			_bar.Value = 0.0;
			AddButton(L.T("إلغاء"), CancelDownload);
			break;
		case "ready":
			_title.Text = L.T("تم التنزيل والتحقق من الملف");
			_sub.Text = L.T("سي\u064fغلق التطبيق ويكمل المثب\u0651ت التحديث.");
			AddButton(L.T("تثبيت الآن"), InstallNow, accent: true);
			AddButton(L.T("لاحق\u064bا"), delegate
			{
				Close();
			});
			break;
		case "error":
			_title.Text = L.T("تعذر تنزيل التحديث");
			_sub.Text = detail ?? "";
			AddButton(L.T("إعادة المحاولة"), Begin, accent: true);
			AddButton(L.T("فتح في المتصفح"), OpenInBrowser);
			break;
		default:
			_title.Text = L.T("تحديث جديد متاح");
			_sub.Text = L.T("الإصدار ") + _version + L.T(" جاهز للتنزيل");
			AddButton(L.T("تنزيل وتثبيت"), Begin, accent: true);
			AddButton(L.T("لاحق\u064bا"), delegate
			{
				Close();
			});
			break;
		}
	}

	public void PreviewState(string state)
	{
		SetState(state, (state == "error") ? L.T("لم يكتمل التنزيل. تحقق من اتصالك بالإنترنت وحاول مرة أخرى.") : null);
		if (state == "downloading")
		{
			_bar.Value = 46.0;
			_sub.Text = "v" + _version + "  46%";
		}
	}

	private void OpenInBrowser()
	{
		try
		{
			Process.Start(new ProcessStartInfo(_fallback)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
		Close();
	}

	private void CancelDownload()
	{
		try
		{
			_cts?.Cancel();
		}
		catch
		{
		}
	}

	public async void Begin()
	{
		if (_cts != null)
		{
			return;
		}
		CancellationTokenSource cancellationTokenSource = (_cts = new CancellationTokenSource());
		SetState("downloading");
		try
		{
			_file = await UpdateInstaller.DownloadAsync(_asset, delegate(double pct)
			{
				_bar.Value = pct;
				_sub.Text = "v" + _version + "  " + (int)pct + "%";
			}, cancellationTokenSource.Token);
			_cts = null;
			SetState("ready");
		}
		catch (OperationCanceledException)
		{
			_cts = null;
			SetState("offer");
		}
		catch (Exception ex2)
		{
			_cts = null;
			try
			{
				Directory.CreateDirectory(App.AppData);
				File.AppendAllText(Path.Combine(App.AppData, "update.log"), DateTime.Now.ToString() + " download: " + ex2.Message + "\n");
			}
			catch
			{
			}
			SetState("error", (ex2 is InvalidDataException) ? L.T("فشل التحقق من الملف المنز\u0651ل. لم يتم تشغيل أي شيء.") : L.T("لم يكتمل التنزيل. تحقق من اتصالك بالإنترنت وحاول مرة أخرى."));
		}
	}

	private async void InstallNow()
	{
		try
		{
			string file = _file;
			if (file == null || !File.Exists(file))
			{
				throw new FileNotFoundException();
			}
			await Task.Run(delegate
			{
				UpdateInstaller.Verify(file, _asset);
			});
			Process.Start(new ProcessStartInfo(file)
			{
				UseShellExecute = true
			});
			Close();
		}
		catch (Exception ex)
		{
			try
			{
				File.AppendAllText(Path.Combine(App.AppData, "update.log"), DateTime.Now.ToString() + " install: " + ex.Message + "\n");
			}
			catch
			{
			}
			_cts = null;
			SetState("error", L.T("فشل التحقق من الملف المنز\u0651ل. لم يتم تشغيل أي شيء."));
		}
	}

	public void ShowToast()
	{
		RectInt32 workArea = DisplayArea.Primary.WorkArea;
		double num = (double)GetDpiForWindow(_hwnd) / 96.0;
		if (num < 1.0)
		{
			num = 1.0;
		}
		int num2 = (int)(380.0 * num);
		int num3 = (int)(190.0 * num);
		RectInt32 rect = new RectInt32(workArea.X + workArea.Width - num2 - (int)(12.0 * num), workArea.Y + workArea.Height - num3 - (int)(12.0 * num), num2, num3);
		base.AppWindow.MoveAndResize(rect);
		ShowWindow(_hwnd, 4);
		base.AppWindow.MoveAndResize(rect);
		WindowChrome.Apply(_hwnd);
		SetWindowPos(_hwnd, new IntPtr(-1), 0, 0, 0, 0, 19u);
		_root.InvalidateMeasure();
		_root.UpdateLayout();
	}
}
