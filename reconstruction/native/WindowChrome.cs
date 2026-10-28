using System;
using System.Runtime.InteropServices;

namespace FluentPrayerTimes;

internal static class WindowChrome
{
	private const int GWL_STYLE = -16;

	private const long WS_CAPTION = 12582912L;

	private const long WS_THICKFRAME = 262144L;

	private const long WS_BORDER = 8388608L;

	private const long WS_DLGFRAME = 4194304L;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOZORDER = 4u;

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_FRAMECHANGED = 32u;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint h, int attr, ref int val, int size);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint h, int i);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint h, int i, nint v);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint h, nint after, int x, int y, int cx, int cy, uint flags);

	public static void Apply(nint hwnd)
	{
		try
		{
			long num = GetWindowLongPtr(hwnd, -16);
			num &= -12845057;
			SetWindowLongPtr(hwnd, -16, (nint)num);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 55u);
		}
		catch
		{
		}
		try
		{
			int val = -2;
			int val2 = 2;
			DwmSetWindowAttribute(hwnd, 34, ref val, 4);
			DwmSetWindowAttribute(hwnd, 33, ref val2, 4);
		}
		catch
		{
		}
	}
}
