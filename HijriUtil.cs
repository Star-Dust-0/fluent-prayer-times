using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FluentPrayerTimes;

public static class HijriUtil
{
    public static readonly UmAlQuraCalendar Cal = new();
    public static readonly string[] HMonths = { "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة" };
    public static readonly string[] GMonths = { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
    // index = (int)DayOfWeek, Sunday = 0
    public static readonly string[] Days = { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
    public static int MinYear => Cal.GetYear(Cal.MinSupportedDateTime) + 1;
    public static int MaxYear => Cal.GetYear(Cal.MaxSupportedDateTime) - 1;

    public static (int y, int m, int d) ToHijri(DateTime dt) => (Cal.GetYear(dt), Cal.GetMonth(dt), Cal.GetDayOfMonth(dt));
    public static DateTime ToGreg(int y, int m, int d) => Cal.ToDateTime(y, m, d, 0, 0, 0, 0);
    public static bool ValidHijri(int y, int m, int d) =>
        y >= MinYear && y <= MaxYear && m >= 1 && m <= 12 && d >= 1 && d <= Cal.GetDaysInMonth(y, m);

    /// <summary>Islamic occasions for a Hijri month/day.</summary>
    public static List<string> Events(int m, int d)
    {
        var l = new List<string>();
        switch (m, d)
        {
            case (1, 1): l.Add("رأس السنة الهجرية"); break;
            case (1, 9): l.Add("يوم تاسوعاء"); break;
            case (1, 10): l.Add("يوم عاشوراء"); break;
            case (3, 12): l.Add("المولد النبوي الشريف"); break;
            case (7, 27): l.Add("ذكرى الإسراء والمعراج"); break;
            case (8, 15): l.Add("ليلة النصف من شعبان"); break;
            case (9, 1): l.Add("أول أيام شهر رمضان المبارك"); break;
            case (9, 21): l.Add("بداية العشر الأواخر من رمضان"); break;
            case (9, 27): l.Add("ليلة السابع والعشرين (يُرجى فيها ليلة القدر)"); break;
            case (10, 1): l.Add("عيد الفطر المبارك"); break;
            case (12, 1): l.Add("بداية العشر الأوائل من ذي الحجة"); break;
            case (12, 9): l.Add("يوم عرفة"); break;
            case (12, 10): l.Add("عيد الأضحى المبارك"); break;
            case (12, 11): l.Add("أول أيام التشريق"); break;
            case (12, 12): l.Add("ثاني أيام التشريق"); break;
            case (12, 13): l.Add("ثالث أيام التشريق"); break;
        }
        if (m != 9 && !(m == 12 && d == 13) && d >= 13 && d <= 15)
            l.Add("صيام أيام البيض (اليوم " + (d == 13 ? "الأول" : d == 14 ? "الثاني" : "الثالث") + ")");
        return l;
    }

    public static Border Card(UIElement child, Thickness? pad = null) => new()
    {
        Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
        BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = pad ?? new Thickness(16), Child = child,
    };
    public static Brush Secondary => (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
    public static Windows.UI.Color Sky => Windows.UI.Color.FromArgb(255, 3, 169, 244);
}
