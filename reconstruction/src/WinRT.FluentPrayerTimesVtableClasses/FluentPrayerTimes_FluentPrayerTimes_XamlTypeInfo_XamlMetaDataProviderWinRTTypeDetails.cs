using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;

namespace WinRT.FluentPrayerTimesVtableClasses;

internal sealed class FluentPrayerTimes_FluentPrayerTimes_XamlTypeInfo_XamlMetaDataProviderWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IXamlMetadataProviderMethods.IID,
				Vtable = IXamlMetadataProviderMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
