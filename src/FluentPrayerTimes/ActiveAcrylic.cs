using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FluentPrayerTimes;

internal sealed class ActiveAcrylic : SystemBackdrop
{
	private DesktopAcrylicController? _c;

	private SystemBackdropConfiguration? _cfg;

	protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		base.OnTargetConnected(target, xamlRoot);
		_c = new DesktopAcrylicController();
		SystemBackdropConfiguration defaultSystemBackdropConfiguration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);
		defaultSystemBackdropConfiguration.IsInputActive = true;
		_cfg = defaultSystemBackdropConfiguration;
		_c.SetSystemBackdropConfiguration(defaultSystemBackdropConfiguration);
		_c.AddSystemBackdropTarget(target);
	}

	protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		if (!(_cfg == null))
		{
			ElementTheme elementTheme = (xamlRoot?.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;
			SystemBackdropConfiguration cfg = _cfg;
			cfg.Theme = elementTheme switch
			{
				ElementTheme.Dark => SystemBackdropTheme.Dark, 
				ElementTheme.Light => SystemBackdropTheme.Light, 
				_ => SystemBackdropTheme.Default, 
			};
		}
	}

	protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
	{
		base.OnTargetDisconnected(target);
		_c?.RemoveSystemBackdropTarget(target);
		_c?.Dispose();
		_c = null;
		_cfg = null;
	}
}
