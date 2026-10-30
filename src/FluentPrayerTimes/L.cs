using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPrayerTimes;

public static class L
{
	public static readonly string[] Codes = new string[11]
	{
		"ar", "en", "fr", "de", "es", "tr", "ur", "id", "ru", "pt",
		"fa"
	};

	public static readonly string[] Native = new string[11]
	{
		"العربية", "English", "Français", "Deutsch", "Español", "Türkçe", "اردو", "Bahasa Indonesia", "Русский", "Português",
		"فارسی"
	};

	private static readonly string[] Cols = new string[10] { "en", "fr", "de", "es", "tr", "ur", "id", "ru", "pt", "fa" };

	private static readonly List<(WeakReference<DependencyObject> el, string prop, string orig)> Tracked = new List<(WeakReference<DependencyObject>, string, string)>();

	private static readonly string[] ArMonths = new string[12]
	{
		"محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال",
		"ذو القعدة", "ذو الحجة"
	};

	public static string Lang { get; private set; } = "ar";

	public static bool Rtl
	{
		get
		{
			if (!(Lang == "ar") && !(Lang == "ur"))
			{
				return Lang == "fa";
			}
			return true;
		}
	}

	public static FlowDirection Flow
	{
		get
		{
			if (!Rtl)
			{
				return FlowDirection.LeftToRight;
			}
			return FlowDirection.RightToLeft;
		}
	}

	public static bool IsArabic => Lang == "ar";

	public static string Detect()
	{
		try
		{
			string text = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
			if (text == "iw")
			{
				text = "he";
			}
			if (text == "in")
			{
				text = "id";
			}
			if (Array.IndexOf(Codes, text) >= 0)
			{
				return text;
			}
		}
		catch
		{
		}
		return "en";
	}

	public static void Set(string setting)
	{
		string text = (string.IsNullOrEmpty(setting) ? Detect() : setting);
		Lang = ((Array.IndexOf(Codes, text) >= 0) ? text : "en");
	}

	public static string T(string ar)
	{
		if (Lang == "ar" || !Strings.Table.TryGetValue(ar, out string[] value))
		{
			return ar;
		}
		int num = Array.IndexOf(Cols, Lang);
		if (num >= 0 && num < value.Length && !string.IsNullOrEmpty(value[num]))
		{
			return value[num];
		}
		if (value.Length != 0 && value[0] != null)
		{
			return value[0];
		}
		return ar;
	}

	public static void Register(object? root)
	{
		if (root == null)
		{
			return;
		}
		if (!(root is TextBlock textBlock))
		{
			if (!(root is TextBox textBox))
			{
				if (!(root is ComboBox comboBox))
				{
					if (!(root is ToggleSwitch toggleSwitch))
					{
						if (!(root is NavigationView navigationView))
						{
							if (!(root is Expander expander))
							{
								if (!(root is ContentControl contentControl))
								{
									if (!(root is Border border))
									{
										if (!(root is Panel panel))
										{
											return;
										}
										{
											foreach (UIElement child in panel.Children)
											{
												Register(child);
											}
											return;
										}
									}
									Register(border.Child);
								}
								else if (contentControl.Content is string orig)
								{
									Track(contentControl, "Content", orig);
								}
								else
								{
									Register(contentControl.Content);
								}
							}
							else
							{
								Track(expander, "ExpanderHeader", expander.Header as string);
                                Register(expander.Header);
								Register(expander.Content);
							}
							return;
						}
						foreach (object menuItem in navigationView.MenuItems)
						{
							Register(menuItem);
						}
						foreach (object footerMenuItem in navigationView.FooterMenuItems)
						{
							Register(footerMenuItem);
						}
						Register(navigationView.Content);
					}
					else
					{
						Track(toggleSwitch, "Header", toggleSwitch.Header as string);
						Track(toggleSwitch, "OnContent", toggleSwitch.OnContent as string);
						Track(toggleSwitch, "OffContent", toggleSwitch.OffContent as string);
					}
				}
				else
				{
					Track(comboBox, "PlaceholderText", comboBox.PlaceholderText);
				}
			}
			else
			{
				Track(textBox, "PlaceholderText", textBox.PlaceholderText);
			}
		}
		else
		{
			Track(textBlock, "Text", textBlock.Text);
		}
	}

    private static void Track(DependencyObject d, string prop, string? orig)
    {
        if (string.IsNullOrEmpty(orig) || !HasArabic(orig)) return;
        // A visual may be reached through more than one content path. Preserve its
        // original source text instead of registering a later translated value.
        foreach (var item in Tracked)
            if (item.prop == prop && item.el.TryGetTarget(out var target) && ReferenceEquals(target, d)) return;
        Tracked.Add((new WeakReference<DependencyObject>(d), prop, orig));
    }

    internal static void WriteDiagnostics()
    {
        try
        {
            var lines = new List<string> { "language=" + Lang };
            foreach (var item in Tracked)
                if (item.el.TryGetTarget(out var target))
                    lines.Add(item.prop + "|" + item.orig + "|expected=" + T(item.orig) + "|actual=" + (target is TextBlock tb ? tb.Text : "[control]"));
            System.IO.File.WriteAllLines(System.IO.Path.Combine(App.AppData, "localization-diagnostics.log"), lines);
        }
        catch { }
    }

	private static bool HasArabic(string s)
	{
		foreach (char c in s)
		{
			if (c >= '\u0600' && c <= 'ۿ')
			{
				return true;
			}
		}
		return false;
	}

	public static void Refresh()
	{
		foreach (var (weakReference, text, ar) in Tracked)
		{
			if (!weakReference.TryGetTarget(out var target))
			{
				continue;
			}
			string text2 = T(ar);
			DependencyObject dependencyObject = target;
			if (dependencyObject is Expander expander && text == "ExpanderHeader")
            {
                expander.Header = text2;
                continue;
            }
            if (!(dependencyObject is TextBlock textBlock))
			{
				if (!(dependencyObject is TextBox textBox))
				{
					if (!(dependencyObject is ComboBox comboBox))
					{
						if (!(dependencyObject is ToggleSwitch toggleSwitch))
						{
							if (dependencyObject is ContentControl contentControl)
							{
								contentControl.Content = text2;
							}
						}
						else if (text == "Header")
						{
							toggleSwitch.Header = text2;
						}
						else if (text == "OnContent")
						{
							toggleSwitch.OnContent = text2;
						}
						else
						{
							toggleSwitch.OffContent = text2;
						}
					}
					else
					{
						comboBox.PlaceholderText = text2;
					}
				}
				else
				{
					textBox.PlaceholderText = text2;
				}
			}
			else if (text == "Text")
			{
				textBlock.Text = text2;
			}
		}
	}

	public static string CountryName(string cc, string ar)
	{
		if (Lang == "ar")
		{
			return ar;
		}
		try
		{
			return new RegionInfo(cc).EnglishName;
		}
		catch
		{
			return ar;
		}
	}

	public static string LocHijri(string h)
	{
		if (Lang == "ar" || string.IsNullOrEmpty(h))
		{
			return h;
		}
		h = h.Replace("ربيع الثاني", "ربيع الآخر").Replace("جمادى الثانية", "جمادى الآخرة").Replace("ذوالقعدة", "ذو القعدة")
			.Replace("ذوالحجة", "ذو الحجة");
		string[] arMonths = ArMonths;
		foreach (string text in arMonths)
		{
			h = h.Replace(text, T(text));
		}
		return h.Replace(" هـ", " " + T("هـ"));
	}
}
