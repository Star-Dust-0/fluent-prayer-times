using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
namespace PrayerTimes;

public class App : Application {
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }

    Settings st = Settings.Load();
    Window win = null!; TextBlock nextName = null!, countdown = null!, dateText = null!, hijriText = null!, offline = null!, locText = null!, gpsStatus = null!, searchStatus = null!;
    StackPanel list = null!; Times.Result? data; string dataDate = ""; TrayIcon tray = null!;
    Border heroBorder = null!, listBorder = null!, rootBorder = null!; Grid stack = null!; StackPanel mainPanel = null!, settingsPanel = null!;
    ComboBox cityBox = null!, dstBox = null!, resultBox = null!; TextBox searchBox = null!, latBox = null!, lonBox = null!;
    List<Times.Place> results = new(); bool loadingUi;
    DateTime lastTrayClick = DateTime.MinValue, lastHide = DateTime.MinValue; bool reloading;
    bool IsDark => ActualThemeVariant == ThemeVariant.Dark;

    public override void Initialize() { Styles.Add(new FluentTheme()); RequestedThemeVariant = ThemeVariant.Default; }
    static Color C(string h) => Color.Parse(h);
    IBrush Br(string l, string d) => new SolidColorBrush(C(IsDark ? d : l));

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d) {
            BuildWindow();
            var menu = new NativeMenu();
            var open = new NativeMenuItem("فتح"); open.Click += (_, _) => ShowWin();
            var sett = new NativeMenuItem("الإعدادات"); sett.Click += (_, _) => { ShowWin(); ShowSettings(true); };
            var quit = new NativeMenuItem("خروج"); quit.Click += (_, _) => d.Shutdown();
            menu.Add(open); menu.Add(sett); menu.Add(new NativeMenuItemSeparator()); menu.Add(quit);
            tray = new TrayIcon { Icon = MakeIcon(), ToolTipText = "مواقيت الصلاة", Menu = menu };
            tray.Clicked += (_, _) => TrayClick();
            TrayIcon.SetIcons(this, new TrayIcons { tray });
            ActualThemeVariantChanged += (_, _) => Render();
            var t = new Thread(() => { while (Program.ShowSignal != null) { Program.ShowSignal.WaitOne(); Dispatcher.UIThread.Post(ShowWin); } }) { IsBackground = true }; t.Start();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; timer.Tick += (_, _) => Render(); timer.Start();
            var re = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) }; re.Tick += async (_, _) => await Reload(); re.Start();
            _ = Reload();
            var shot = Environment.GetEnvironmentVariable("PRAYER_SHOT");
            if (shot != null) { ShowWin(); if (Environment.GetEnvironmentVariable("PRAYER_SETTINGS") == "1") ShowSettings(true); }
            else if (Environment.GetCommandLineArgs().Contains("--show")) ShowWin();
        }
        base.OnFrameworkInitializationCompleted();
    }

    WindowIcon MakeIcon() {
        const int S = 64;
        var bmp = new WriteableBitmap(new PixelSize(S, S), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = bmp.Lock()) {
            var px = new byte[S * S * 4];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) {
                double dx = x - 31.5, dy = y - 31.5, ex = x - 42, ey = y - 24;
                if (dx * dx + dy * dy <= 26 * 26 && ex * ex + ey * ey > 22 * 22) { int o = (y * S + x) * 4; px[o] = 255; px[o + 1] = 255; px[o + 2] = 255; px[o + 3] = 255; }
            }
            Marshal.Copy(px, 0, fb.Address, px.Length);
        }
        var ms = new MemoryStream(); bmp.Save(ms); ms.Position = 0; return new WindowIcon(ms);
    }

    TextBlock Lbl(string t) => new() { Text = t, FontSize = 13, Opacity = .7, Margin = new Thickness(2, 12, 2, 4) };
    Button Btn(string t, Action a) { var b = new Button { Content = t, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center }; b.Click += (_, _) => a(); return b; }

    void BuildWindow() {
        nextName = new TextBlock { FontSize = 22, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
        countdown = new TextBlock { FontSize = 44, FontWeight = FontWeight.Light, HorizontalAlignment = HorizontalAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
        var lbl = new TextBlock { Text = "الصلاة القادمة", FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center, Opacity = .65 };
        dateText = new TextBlock { FontSize = 13, Opacity = .75 }; hijriText = new TextBlock { FontSize = 14, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 12) };
        locText = new TextBlock { FontSize = 13, Opacity = .75 };
        offline = new TextBlock { FontSize = 12, Opacity = .65, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        list = new StackPanel();
        heroBorder = new Border { CornerRadius = new CornerRadius(12), Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 14), BorderThickness = new Thickness(1), Child = new StackPanel { Spacing = 2, Children = { lbl, nextName, countdown } } };
        listBorder = new Border { CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), ClipToBounds = true, Child = list };
        var gear = new Button { Content = "⚙", FontSize = 18, Background = Brushes.Transparent, Padding = new Thickness(8, 2), HorizontalAlignment = HorizontalAlignment.Left };
        gear.Click += (_, _) => ShowSettings(true);
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(4, 0, 0, 2) };
        head.Children.Add(new TextBlock { Text = "مواقيت الصلاة", FontSize = 20, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(gear, 1); head.Children.Add(gear);
        mainPanel = new StackPanel { Children = { head, new StackPanel { Margin = new Thickness(4, 0, 4, 0), Children = { locText, dateText, hijriText } }, heroBorder, listBorder, offline } };

        // settings panel
        cityBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, ItemsSource = Cities.All.Select(c => c.Ar).ToList() };
        cityBox.SelectionChanged += (_, _) => { if (loadingUi || cityBox.SelectedIndex < 0) return; var c = Cities.All[cityBox.SelectedIndex]; ApplyLoc(new Loc { Name = c.Name, NameAr = c.Ar, Lat = c.Lat, Lon = c.Lon, UseCity = true }); };
        dstBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, ItemsSource = new[] { "تلقائي (حسب قواعد مصر)", "إيقاف - توقيت شتوي (UTC+2)", "تشغيل - توقيت صيفي (UTC+3)" } };
        dstBox.SelectionChanged += (_, _) => { if (loadingUi) return; st.Dst = new[] { "auto", "off", "on" }[Math.Max(0, dstBox.SelectedIndex)]; st.Save(); _ = Reload(); };
        searchBox = new TextBox { Watermark = "اسم المدينة أو القرية", HorizontalAlignment = HorizontalAlignment.Stretch };
        searchStatus = new TextBlock { FontSize = 12, Opacity = .7, TextWrapping = TextWrapping.Wrap };
        resultBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, IsVisible = false };
        resultBox.SelectionChanged += (_, _) => { if (resultBox.SelectedIndex < 0 || resultBox.SelectedIndex >= results.Count) return; var p = results[resultBox.SelectedIndex]; ApplyLoc(new Loc { Name = p.Label, NameAr = p.Label, Lat = p.Lat, Lon = p.Lon, UseCity = false }); };
        latBox = new TextBox { Watermark = "خط العرض 31.2", FlowDirection = FlowDirection.LeftToRight }; lonBox = new TextBox { Watermark = "خط الطول 29.9", FlowDirection = FlowDirection.LeftToRight };
        gpsStatus = new TextBlock { FontSize = 12, Opacity = .7, TextWrapping = TextWrapping.Wrap };
        var back = Btn("رجوع", () => ShowSettings(false));
        settingsPanel = new StackPanel { IsVisible = false, Spacing = 2, Children = {
            new TextBlock { Text = "الإعدادات", FontSize = 20, FontWeight = FontWeight.SemiBold, Margin = new Thickness(4, 0, 0, 4) },
            Lbl("المدينة (قائمة مدن مصر)"), cityBox,
            Lbl("التوقيت الصيفي"), dstBox,
            Lbl("بحث عن موقع آخر في مصر (من خدمة خرائط)"), searchBox, Btn("بحث", async () => await DoSearch()), resultBox, searchStatus,
            Lbl("إدخال إحداثيات يدويًا"), new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*"), Children = { latBox, WithCol(lonBox, 2) } }, Btn("تطبيق الإحداثيات", ApplyCoords),
            Lbl("تحديد موقعي تلقائيًا"), Btn("تحديد موقعي (GPS / IP)", async () => await DoDetect()), gpsStatus,
            new Border { Height = 8 }, back } };
        stack = new Grid { Margin = new Thickness(20), Children = { mainPanel, settingsPanel } };
        rootBorder = new Border { Child = stack, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1) };
        win = new Window { Title = "مواقيت الصلاة", Width = 380, Height = 740, CanResize = false, SystemDecorations = SystemDecorations.None, Topmost = true, ShowInTaskbar = false,
            Background = Brushes.Transparent, FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Segoe UI Variable, Segoe UI, Tahoma, sans-serif"), Content = rootBorder };
        win.Closing += (_, e) => { e.Cancel = true; HideWin(); };
        win.Deactivated += (_, _) => { if (Environment.GetEnvironmentVariable("PRAYER_SHOT") == null) HideWin(); };
        win.KeyDown += (_, e) => { if (e.Key == Avalonia.Input.Key.Escape) HideWin(); };
        SyncSettingsUi();
    }
    static Control WithCol(Control c, int col) { Grid.SetColumn(c, col); return c; }

    void SyncSettingsUi() {
        loadingUi = true;
        int i = st.Location.UseCity ? Array.FindIndex(Cities.All, c => c.Name == st.Location.Name) : -1;
        cityBox.SelectedIndex = i; dstBox.SelectedIndex = st.Dst switch { "off" => 1, "on" => 2, _ => 0 };
        latBox.Text = st.Location.Lat.ToString("F4", CultureInfo.InvariantCulture); lonBox.Text = st.Location.Lon.ToString("F4", CultureInfo.InvariantCulture);
        loadingUi = false;
    }
    void ShowSettings(bool on) { mainPanel.IsVisible = !on; settingsPanel.IsVisible = on; if (on) SyncSettingsUi(); }
    void ApplyLoc(Loc l) { st.Location = l; st.Save(); SyncSettingsUi(); data = null; _ = Reload(); }
    void ApplyCoords() {
        if (double.TryParse(latBox.Text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var la) && double.TryParse(lonBox.Text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lo) && Math.Abs(la) <= 90 && Math.Abs(lo) <= 180)
        { ApplyLoc(new Loc { Name = "manual", NameAr = $"{la:F2}, {lo:F2}", Lat = la, Lon = lo, UseCity = false }); gpsStatus.Text = "تم تطبيق الإحداثيات"; }
        else gpsStatus.Text = "إحداثيات غير صحيحة";
    }
    async Task DoSearch() {
        var q = searchBox.Text?.Trim(); if (string.IsNullOrEmpty(q)) return;
        searchStatus.Text = "جاري البحث...";
        try { results = await Times.Search(q); resultBox.ItemsSource = results.Select(r => r.Label).ToList(); resultBox.IsVisible = results.Count > 0; searchStatus.Text = results.Count == 0 ? "لا توجد نتائج" : "اختر من النتائج"; }
        catch { searchStatus.Text = "تعذر البحث - تأكد من الاتصال بالإنترنت"; }
    }
    async Task DoDetect() {
        gpsStatus.Text = "جاري تحديد الموقع...";
        var (p, how) = await Times.Detect();
        if (p == null) { gpsStatus.Text = "تعذر تحديد الموقع. فعّل خدمة الموقع في ويندوز أو أدخل الموقع يدويًا"; return; }
        ApplyLoc(new Loc { Name = "gps", NameAr = $"{p.Label}", Lat = p.Lat, Lon = p.Lon, UseCity = false });
        gpsStatus.Text = $"تم التحديد عن طريق: {how} ({p.Lat:F3}, {p.Lon:F3})";
    }

    void PositionWin() {
        var scr = win.Screens.Primary; if (scr == null) return;
        double sc = scr.Scaling; var wa = scr.WorkingArea;
        int w = (int)(win.Width * sc), h = (int)(win.Height * sc), m = (int)(12 * sc);
        int x = wa.Right - w - m, y = wa.Bottom - h - m;   // bottom-right corner above the taskbar
        if (OperatingSystem.IsWindows() && GetCursorPos(out var p)) {
            var s2 = win.Screens.ScreenFromPoint(new PixelPoint(p.X, p.Y)) ?? scr; wa = s2.WorkingArea;
            x = Math.Clamp(p.X - w / 2, wa.X + m, wa.Right - w - m);
            y = Math.Clamp(p.Y - h - m, wa.Y + m, wa.Bottom - h - m);
        }
        win.Position = new PixelPoint(x, y);
    }
    void ShowWin() { ShowSettings(false); PositionWin(); win.Show(); win.Activate(); _ = Reload(); }
    void HideWin() { if (win.IsVisible) { win.Hide(); lastHide = DateTime.Now; } }
    void TrayClick() {
        var n = DateTime.Now; if ((n - lastTrayClick).TotalMilliseconds < 400) return; lastTrayClick = n;
        if (win.IsVisible) HideWin();
        else if ((n - lastHide).TotalMilliseconds > 400) ShowWin();   // clicking the tray steals focus and hides the window first
    }
    async Task Reload() {
        if (reloading) return; reloading = true;
        try { data = await Times.Load(st); dataDate = DateTime.Now.ToString("yyyy-MM-dd"); Render(); } finally { reloading = false; }
    }

    void Render() {
        rootBorder.Background = Br("#F3F3F3", "#202020"); rootBorder.BorderBrush = Br("#D0D0D0", "#444444");
        var text = Br("#1A1A1A", "#FFFFFF"); var sub = Br("#605E5C", "#A0A0A0"); var card = Br("#FFFFFF", "#2B2B2B"); var line = Br("#E5E5E5", "#3A3A3A");
        var acc = Br("#0067C0", "#60CDFF"); var accBg = Br("#E5F1FB", "#0F3550");
        heroBorder.Background = accBg; heroBorder.BorderBrush = line; listBorder.Background = card; listBorder.BorderBrush = line; win.Foreground = text;
        locText.Text = "الموقع: " + st.Location.NameAr; locText.Foreground = text;
        if (data?.Today == null) { offline.Text = data == null ? "" : "تعذر تحميل المواقيت - تأكد من الاتصال بالإنترنت"; tray.ToolTipText = "مواقيت الصلاة"; return; }
        var now = DateTime.Now; var today = data.Today.T;
        if (dataDate != now.ToString("yyyy-MM-dd")) { _ = Reload(); }
        var nx = Times.Next(now, today, data.Tomorrow?.T);
        list.Children.Clear();
        for (int i = 0; i < 6; i++) {
            bool isNext = nx is { tomorrow: false } n1 && n1.idx == i; bool past = Times.At(now, today[Times.Keys[i]]) <= now;
            var row = new Border { Padding = new Thickness(18, 13), Background = isNext ? accBg : Brushes.Transparent, BorderBrush = line, BorderThickness = new Thickness(0, 0, 0, i < 5 ? 1 : 0), Opacity = past && !isNext ? .5 : 1 };
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var name = new TextBlock { Text = Times.Names[i], FontSize = 16, FontWeight = isNext ? FontWeight.SemiBold : FontWeight.Normal, Foreground = text };
            var tm = new TextBlock { Text = Times.F12(today[Times.Keys[i]]), FontSize = 16, FlowDirection = FlowDirection.LeftToRight, Foreground = isNext ? acc : sub, FontWeight = isNext ? FontWeight.SemiBold : FontWeight.Normal };
            Grid.SetColumn(tm, 1); g.Children.Add(name); g.Children.Add(tm); row.Child = g; list.Children.Add(row);
        }
        string nm = nx == null ? "--" : Times.Names[nx.Value.idx]; string cd = nx == null ? "--:--:--" : Times.Fmt(nx.Value.time - now);
        nextName.Foreground = acc; countdown.Foreground = text;
        nextName.Text = nm + (nx is { tomorrow: true } ? " (غدًا)" : ""); countdown.Text = cd;
        tray.ToolTipText = nx == null ? "مواقيت الصلاة" : $"باقي على صلاة {nm}: {cd}";
        dateText.Foreground = sub; dateText.Text = now.ToString("dddd d MMMM yyyy", new CultureInfo("ar-EG") { NumberFormat = CultureInfo.InvariantCulture.NumberFormat, DateTimeFormat = { Calendar = new GregorianCalendar() } });
        hijriText.Foreground = acc; hijriText.Text = data.Today.Hijri;
        offline.Foreground = sub; offline.Text = data.Offline ? "غير متصل - يتم عرض آخر مواقيت محفوظة" : "";
    }
}
