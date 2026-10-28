using System;
using System.Runtime.InteropServices;

namespace FluentPrayerTimes;

internal sealed class TrayIcon : IDisposable
{
	private delegate nint SubclassProc(nint hWnd, uint msg, nint wParam, nint lParam, nuint id, nuint data);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NOTIFYICONDATA
	{
		public uint cbSize;

		public nint hWnd;

		public uint uID;

		public uint uFlags;

		public uint uCallbackMessage;

		public nint hIcon;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szTip;

		public uint dwState;

		public uint dwStateMask;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string szInfo;

		public uint uVersion;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string szInfoTitle;

		public uint dwInfoFlags;

		public Guid guidItem;

		public nint hBalloonIcon;
	}

	private struct POINT
	{
		public int x;

		public int y;
	}

	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private struct NOTIFYICONIDENTIFIER
	{
		public uint cbSize;

		public nint hWnd;

		public uint uID;

		public Guid guidItem;
	}

	private const uint WM_TRAY = 32769u;

	private const uint WM_LBUTTONUP = 514u;

	private const uint WM_RBUTTONUP = 517u;

	private const uint WM_MOUSEMOVE = 512u;

	private const uint NIM_ADD = 0u;

	private const uint NIM_MODIFY = 1u;

	private const uint NIM_DELETE = 2u;

	private const uint NIF_MESSAGE = 1u;

	private const uint NIF_ICON = 2u;

	private const uint NIF_TIP = 4u;

	private const uint NIF_INFO = 16u;

	public bool NativeTip;

	private int _lastX = int.MinValue;

	private int _lastY = int.MinValue;

	private readonly nint _hwnd;

	private readonly Action _onClick;

	private readonly Action<int> _onMenu;

	private readonly Func<int, int, bool>? _onContext;

	public Action? OnHover;

	public Action? OnSystemThemeChanged;

	private readonly (int id, string text)[] _items;

	private readonly SubclassProc _proc;

	private readonly uint _taskbarCreated;

	private nint _icon;

	private string _tip = "";

	private bool _added;

	[DllImport("shell32.dll")]
	private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER id, out RECT r);

	public bool TryGetAnchor(out int x, out int y)
	{
		try
		{
			NOTIFYICONIDENTIFIER id = new NOTIFYICONIDENTIFIER
			{
				hWnd = _hwnd,
				uID = 1u
			};
			id.cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>();
			if (Shell_NotifyIconGetRect(ref id, out var r) == 0 && r.right > r.left && r.bottom > r.top)
			{
				x = (r.left + r.right) / 2;
				y = (r.top + r.bottom) / 2;
				return true;
			}
		}
		catch
		{
		}
		if (_lastX != int.MinValue)
		{
			x = _lastX;
			y = _lastY;
			return true;
		}
		x = 0;
		y = 0;
		return false;
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern bool Shell_NotifyIconW(uint msg, ref NOTIFYICONDATA d);

	[DllImport("comctl32.dll")]
	private static extern bool SetWindowSubclass(nint h, SubclassProc p, nuint id, nuint data);

	[DllImport("comctl32.dll")]
	private static extern bool RemoveWindowSubclass(nint h, SubclassProc p, nuint id);

	[DllImport("comctl32.dll")]
	private static extern nint DefSubclassProc(nint h, uint m, nint w, nint l);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint LoadImageW(nint inst, string name, uint type, int cx, int cy, uint flags);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int i);

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(nint h);

	[DllImport("user32.dll")]
	private static extern nint CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool AppendMenuW(nint m, uint flags, nuint id, string text);

	[DllImport("user32.dll")]
	private static extern int TrackPopupMenu(nint m, uint flags, int x, int y, int r, nint hwnd, nint rc);

	[DllImport("user32.dll")]
	private static extern bool DestroyMenu(nint m);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT p);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint h);

	[DllImport("user32.dll")]
	private static extern bool PostMessageW(nint h, uint m, nint w, nint l);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern uint RegisterWindowMessageW(string s);

	public TrayIcon(nint hwnd, string icoPath, (int id, string text)[] items, Action onClick, Action<int> onMenu, Func<int, int, bool>? onContext = null)
	{
		_hwnd = hwnd;
		_items = items;
		_onClick = onClick;
		_onMenu = onMenu;
		_onContext = onContext;
		int num = GetSystemMetrics(49);
		if (num <= 0)
		{
			num = 16;
		}
		_icon = LoadImageW(IntPtr.Zero, icoPath, 1u, num, num, 16u);
		_taskbarCreated = RegisterWindowMessageW("TaskbarCreated");
		_proc = WndProc;
		SetWindowSubclass(_hwnd, _proc, new UIntPtr(1u), UIntPtr.Zero);
		Add();
	}

	private NOTIFYICONDATA Data(uint flags)
	{
		NOTIFYICONDATA result = new NOTIFYICONDATA
		{
			hWnd = _hwnd,
			uID = 1u,
			uFlags = flags,
			uCallbackMessage = 32769u,
			hIcon = _icon,
			szTip = _tip,
			szInfo = "",
			szInfoTitle = ""
		};
		result.cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>();
		return result;
	}

	private void Add()
	{
		NOTIFYICONDATA d = Data(7u);
		_added = Shell_NotifyIconW(0u, ref d);
	}

	public void Notify(string title, string text, bool silent = false)
	{
		if (_added)
		{
			if (title.Length > 60)
			{
				title = title.Substring(0, 60);
			}
			if (text.Length > 250)
			{
				text = text.Substring(0, 250);
			}
			NOTIFYICONDATA d = Data(16u);
			d.szInfoTitle = title;
			d.szInfo = text;
			d.dwInfoFlags = (silent ? 16u : 0u);
			Shell_NotifyIconW(1u, ref d);
		}
	}

	public void SetIcon(string icoPath)
	{
		try
		{
			int num = GetSystemMetrics(49);
			if (num <= 0)
			{
				num = 16;
			}
			nint num2 = LoadImageW(IntPtr.Zero, icoPath, 1u, num, num, 16u);
			if (num2 != IntPtr.Zero)
			{
				nint icon = _icon;
				_icon = num2;
				if (_added)
				{
					NOTIFYICONDATA d = Data(2u);
					Shell_NotifyIconW(1u, ref d);
				}
				if (icon != IntPtr.Zero)
				{
					DestroyIcon(icon);
				}
			}
		}
		catch
		{
		}
	}

	public void SetTip(string tip)
	{
		if (!NativeTip)
		{
			return;
		}
		if (tip.Length > 120)
		{
			tip = tip.Substring(0, 120);
		}
		if (!(tip == _tip))
		{
			_tip = tip;
			if (_added)
			{
				NOTIFYICONDATA d = Data(4u);
				Shell_NotifyIconW(1u, ref d);
			}
		}
	}

	private nint WndProc(nint h, uint msg, nint w, nint l, nuint id, nuint data)
	{
		if (msg == 32769)
		{
			uint num = (uint)((ulong)l & 0xFFFFuL);
			if (GetCursorPos(out var p))
			{
				_lastX = p.x;
				_lastY = p.y;
			}
			switch (num)
			{
			case 512u:
				try
				{
					OnHover?.Invoke();
				}
				catch
				{
				}
				break;
			case 514u:
				_onClick();
				break;
			case 517u:
			{
				bool flag = false;
				if (_onContext != null && GetCursorPos(out var p2))
				{
					try
					{
						flag = _onContext(p2.x, p2.y);
					}
					catch
					{
					}
				}
				if (!flag)
				{
					ShowMenu();
				}
				break;
			}
			}
			return IntPtr.Zero;
		}
		if (_taskbarCreated != 0 && msg == _taskbarCreated)
		{
			Add();
		}
		if (msg == 26)
		{
			try
			{
				OnSystemThemeChanged?.Invoke();
			}
			catch
			{
			}
		}
		return DefSubclassProc(h, msg, w, l);
	}

	private void ShowMenu()
	{
		GetCursorPos(out var p);
		nint m = CreatePopupMenu();
		(int, string)[] items = _items;
		for (int i = 0; i < items.Length; i++)
		{
			var (value, text) = items[i];
			AppendMenuW(m, 0u, new UIntPtr((uint)value), text);
		}
		SetForegroundWindow(_hwnd);
		int num = TrackPopupMenu(m, 290u, p.x, p.y, 0, _hwnd, IntPtr.Zero);
		PostMessageW(_hwnd, 0u, IntPtr.Zero, IntPtr.Zero);
		DestroyMenu(m);
		if (num > 0)
		{
			_onMenu(num);
		}
	}

	public void Dispose()
	{
		if (_added)
		{
			NOTIFYICONDATA d = Data(0u);
			Shell_NotifyIconW(2u, ref d);
			_added = false;
		}
		RemoveWindowSubclass(_hwnd, _proc, new UIntPtr(1u));
		if (_icon != IntPtr.Zero)
		{
			DestroyIcon(_icon);
			_icon = IntPtr.Zero;
		}
	}
}
