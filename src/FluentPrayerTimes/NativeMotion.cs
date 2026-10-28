using System;
using System.Runtime.InteropServices;

namespace FluentPrayerTimes;

internal static class NativeMotion
{
	[DllImport("user32.dll")]
	private static extern bool SystemParametersInfoW(uint action, uint param, out int value, uint flags);

	[DllImport("user32.dll")]
	private static extern bool AnimateWindow(nint hwnd, uint duration, uint flags);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hwnd, int command);

	[DllImport("user32.dll")]
	private static extern uint GetDoubleClickTime();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreateWindowExW(uint ex, string cls, string? title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);

	[DllImport("user32.dll")]
	private static extern nint SendMessageW(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hwnd);

	private static bool Enabled(uint action)
	{
		if (SystemParametersInfoW(action, 0u, out var value, 0u))
		{
			return value != 0;
		}
		return false;
	}

	public static int HoverDelay()
	{
		nint num = CreateWindowExW(0u, "tooltips_class32", null, 0u, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		try
		{
			int num2 = (int)((num != IntPtr.Zero) ? SendMessageW(num, 1045u, new IntPtr(3), IntPtr.Zero) : 0);
			return (num2 > 0) ? num2 : ((int)GetDoubleClickTime());
		}
		finally
		{
			if (num != IntPtr.Zero)
			{
				DestroyWindow(num);
			}
		}
	}

	public static bool TooltipAnimations()
	{
		if (Enabled(4162u))
		{
			return Enabled(4118u);
		}
		return false;
	}

	public static void Tooltip(nint hwnd, bool show, bool below)
	{
		bool num = Enabled(4162u) && Enabled(4118u);
		uint num2 = (Enabled(4120u) ? 524288u : ((uint)(0x40000 | ((below == show) ? 4 : 8))));
		if (!num || !AnimateWindow(hwnd, 160u, num2 | (uint)((!show) ? 65536 : 0)))
		{
			ShowWindow(hwnd, show ? 4 : 0);
		}
	}

	public static void Window(nint hwnd, bool show)
	{
		if (!Enabled(4162u) || !AnimateWindow(hwnd, 180u, (uint)(0x10 | (show ? 131072 : 65536))))
		{
			ShowWindow(hwnd, show ? 5 : 0);
		}
	}
}
