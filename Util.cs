using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using WShapes = System.Windows.Shapes;

namespace Cascade;

public static class Util
{
    public static string Fmt(long ms)
    {
        long s = Math.Max(0, ms / 1000);
        long h = s / 3600, m = s % 3600 / 60, x = s % 60;
        return h > 0 ? $"{h}:{m:00}:{x:00}" : $"{m:00}:{x:00}";
    }

    public static string Human(long b) =>
        b >= (1L << 30) ? $"{b / (double)(1L << 30):0.0} GB" : b >= (1L << 20) ? $"{b >> 20} MB" : $"{b >> 10} KB";

    public static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public static SolidColorBrush Brush(string hex)
    {
        var b = new SolidColorBrush(C(hex));
        b.Freeze();
        return b;
    }

    [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int StrCmpLogicalW(string a, string b);

    /// <summary>Explorer-style natural sort (Episode 2 before Episode 10).</summary>
    public static int NatCmp(string a, string b) => StrCmpLogicalW(a ?? "", b ?? "");

    public static string SpeedLabel(double r) => Math.Round(r, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x";
}

public static class Theme
{
    public static bool Dark = true;

    static bool SystemDark()
    {
        try
        {
            var v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return v is int i && i == 0;
        }
        catch { return true; }
    }

    public static void Apply()
    {
        var t = Store.Cfg.Theme;
        Dark = t == "dark" || (t == "system" && SystemDark());
        var r = Application.Current.Resources;
        void P(string key, string hex) => r[key] = Util.Brush(hex);
        P("Bg", Dark ? "#0E0F13" : "#F6F3F0");
        r["BgColor"] = Util.C(Dark ? "#0E0F13" : "#F6F3F0");
        P("Fg", Dark ? "#F2F2F5" : "#16171B");
        P("Muted", Dark ? "#A9ABB5" : "#6B6D78");
        P("Glass", Dark ? "#14FFFFFF" : "#A6FFFFFF");
        P("GlassBorder", Dark ? "#21FFFFFF" : "#12000000");
        P("Surface", Dark ? "#1B1C22" : "#FFFFFF");
        P("TopBar", Dark ? "#8C0E0F13" : "#99F6F3F0");
        P("MenuBg", Dark ? "#1B1C22" : "#FFFFFF");
    }
}

public static class Icons
{
    public static WShapes.Path Make(string name, double size, string resKey = "Fg", Brush fill = null)
    {
        var p = new WShapes.Path
        {
            Data = Geometry.Parse(IconData.Paths[name]),
            Stretch = Stretch.Uniform,
            Width = size,
            Height = size,
            IsHitTestVisible = false,
        };
        if (fill != null) p.Fill = fill; else p.SetResourceReference(WShapes.Shape.FillProperty, resKey);
        return p;
    }
}
