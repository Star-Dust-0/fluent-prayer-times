using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
namespace FluentPrayerTimes;
internal static class ControlIds
{
    internal static void Set(DependencyObject control, string id) => AutomationProperties.SetAutomationId(control, id);
}
