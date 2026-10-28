using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;

namespace WinRT.FluentPrayerTimesVtableClasses;

internal sealed class FluentPrayerTimes_WidgetWindowWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IComponentConnectorMethods.IID,
				Vtable = IComponentConnectorMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
