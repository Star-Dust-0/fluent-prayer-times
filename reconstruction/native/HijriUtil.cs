using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.UI;

namespace FluentPrayerTimes;

public static class HijriUtil
{
	public static readonly UmAlQuraCalendar Cal = new UmAlQuraCalendar();

	private const string NS = "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'";

	public static string[] HMonths => new string[12]
	{
		L.T("محرم"),
		L.T("صفر"),
		L.T("ربيع الأول"),
		L.T("ربيع الآخر"),
		L.T("جمادى الأولى"),
		L.T("جمادى الآخرة"),
		L.T("رجب"),
		L.T("شعبان"),
		L.T("رمضان"),
		L.T("شوال"),
		L.T("ذو القعدة"),
		L.T("ذو الحجة")
	};

	public static string[] GMonths => new string[12]
	{
		L.T("يناير"),
		L.T("فبراير"),
		L.T("مارس"),
		L.T("أبريل"),
		L.T("مايو"),
		L.T("يونيو"),
		L.T("يوليو"),
		L.T("أغسطس"),
		L.T("سبتمبر"),
		L.T("أكتوبر"),
		L.T("نوفمبر"),
		L.T("ديسمبر")
	};

	public static string[] Days => new string[7]
	{
		L.T("الأحد"),
		L.T("الاثنين"),
		L.T("الثلاثاء"),
		L.T("الأربعاء"),
		L.T("الخميس"),
		L.T("الجمعة"),
		L.T("السبت")
	};

	public static int MinYear => Cal.GetYear(Cal.MinSupportedDateTime) + 1;

	public static int MaxYear => Cal.GetYear(Cal.MaxSupportedDateTime) - 1;

	public static Color Sky => Color.FromArgb(byte.MaxValue, 3, 169, 244);

	public static (int y, int m, int d) ToHijri(DateTime dt)
	{
		return (y: Cal.GetYear(dt), m: Cal.GetMonth(dt), d: Cal.GetDayOfMonth(dt));
	}

	public static DateTime ToGreg(int y, int m, int d)
	{
		return Cal.ToDateTime(y, m, d, 0, 0, 0, 0);
	}

	public static bool ValidHijri(int y, int m, int d)
	{
		if (y >= MinYear && y <= MaxYear && m >= 1 && m <= 12 && d >= 1)
		{
			return d <= Cal.GetDaysInMonth(y, m);
		}
		return false;
	}

	public static List<string> Events(int m, int d)
	{
		List<string> list = new List<string>();
		switch (m)
		{
		case 1:
			switch (d)
			{
			case 1:
				list.Add(L.T("رأس السنة الهجرية"));
				break;
			case 9:
				list.Add(L.T("يوم تاسوعاء"));
				break;
			case 10:
				list.Add(L.T("يوم عاشوراء"));
				break;
			}
			break;
		case 3:
			if (d == 12)
			{
				list.Add(L.T("المولد النبوي الشريف"));
			}
			break;
		case 7:
			if (d == 27)
			{
				list.Add(L.T("ذكرى الإسراء والمعراج"));
			}
			break;
		case 8:
			if (d == 15)
			{
				list.Add(L.T("ليلة النصف من شعبان"));
			}
			break;
		case 9:
			switch (d)
			{
			case 1:
				list.Add(L.T("أول أيام شهر رمضان المبارك"));
				break;
			case 21:
				list.Add(L.T("بداية العشر الأواخر من رمضان"));
				break;
			case 27:
				list.Add(L.T("ليلة السابع والعشرين (ي\u064fرجى فيها ليلة القدر)"));
				break;
			}
			break;
		case 10:
			if (d == 1)
			{
				list.Add(L.T("عيد الفطر المبارك"));
			}
			break;
		case 12:
			switch (d)
			{
			case 1:
				list.Add(L.T("بداية العشر الأوائل من ذي الحجة"));
				break;
			case 9:
				list.Add(L.T("يوم عرفة"));
				break;
			case 10:
				list.Add(L.T("عيد الأضحى المبارك"));
				break;
			case 11:
				list.Add(L.T("أول أيام التشريق"));
				break;
			case 12:
				list.Add(L.T("ثاني أيام التشريق"));
				break;
			case 13:
				list.Add(L.T("ثالث أيام التشريق"));
				break;
			}
			break;
		}
		if (m != 9 && (m != 12 || d != 13) && d >= 13 && d <= 15)
		{
			list.Add(L.T("صيام أيام البيض (اليوم ") + d switch
			{
				14 => L.T("الثاني"), 
				13 => L.T("الأول"), 
				_ => L.T("الثالث"), 
			} + ")");
		}
		return list;
	}

	public static Border Card(UIElement child, Thickness? pad = null)
	{
		Border obj = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource CardBackgroundFillColorDefaultBrush}' BorderBrush='{ThemeResource CardStrokeColorDefaultBrush}' BorderThickness='1' CornerRadius='8'/>");
		obj.Padding = pad ?? new Thickness(16.0);
		obj.Child = child;
		return obj;
	}

	public static Border Themed(string bgBrushKey, double opacity = 1.0)
	{
		return (Border)XamlReader.Load($"<Border {"xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'"} Background='{{ThemeResource {bgBrushKey}}}' Opacity='{opacity.ToString(CultureInfo.InvariantCulture)}'/>");
	}

	public static TextBlock Themed(TextBlock s, string fgBrushKey)
	{
		TextBlock obj = (TextBlock)XamlReader.Load($"<TextBlock {"xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'"} Foreground='{{ThemeResource {fgBrushKey}}}'/>");
		obj.Text = s.Text;
		obj.FontSize = s.FontSize;
		obj.FontWeight = s.FontWeight;
		obj.HorizontalAlignment = s.HorizontalAlignment;
		obj.VerticalAlignment = s.VerticalAlignment;
		obj.TextWrapping = s.TextWrapping;
		obj.TextAlignment = s.TextAlignment;
		obj.Margin = s.Margin;
		obj.TextTrimming = s.TextTrimming;
		obj.FlowDirection = s.FlowDirection;
		return obj;
	}

	public static TextBlock Sec(TextBlock s)
	{
		return Themed(s, "TextFillColorSecondaryBrush");
	}
}
