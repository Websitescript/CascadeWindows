using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;

namespace Cascade;

public class Settings
{
    public string Theme { get; set; } = "system";   // system | light | dark
    public int Seek { get; set; } = 10;
    public List<string> Folders { get; set; } = new();
    public string Sort { get; set; } = "new";
    public int Volume { get; set; } = 100;           // 0..200 (VLC software gain)
    public int Repeat { get; set; }                  // 0 off, 1 one, 2 all
    public int HoldSpeedX10 { get; set; } = 20;     // hold-to-speed, x10
}

public class Prog
{
    public long Pos { get; set; }
    public long Dur { get; set; }
    public long T { get; set; }
}

public class MediaItem
{
    public string FullPath { get; set; }
    public string Title { get; set; }
    public string Folder { get; set; }
    public string FolderName { get; set; }
    public long Size { get; set; }
    public DateTime Mtime { get; set; }
    public bool IsAudio { get; set; }
    public bool IsUrl { get; set; }
}

public static class Store
{
    public static string DataDir, ThumbDir;
    public static bool Portable;
    public static Settings Cfg = new();
    public static Dictionary<string, Prog> Progress = new();
    public static Dictionary<string, long> Durations = new();
    static readonly object Gate = new();

    public static void Init()
    {
        Portable = File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "portable.txt"));
        DataDir = Portable
            ? System.IO.Path.Combine(AppContext.BaseDirectory, "CascadeData")
            : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cascade");
        ThumbDir = System.IO.Path.Combine(DataDir, "thumbs");
        Directory.CreateDirectory(ThumbDir);
        Cfg = Read<Settings>("settings.json") ?? new Settings();
        Progress = Read<Dictionary<string, Prog>>("progress.json") ?? new();
        Durations = Read<Dictionary<string, long>>("durations.json") ?? new();
        if (Cfg.Folders.Count == 0)
        {
            foreach (var f in new[] { Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) })
                if (!string.IsNullOrEmpty(f) && Directory.Exists(f)) Cfg.Folders.Add(f);
            SaveCfg();
        }
    }

    static T Read<T>(string name) where T : class
    {
        try
        {
            var p = System.IO.Path.Combine(DataDir, name);
            if (File.Exists(p)) return JsonSerializer.Deserialize<T>(File.ReadAllText(p));
        }
        catch { }
        return null;
    }

    static void Write(string name, object o)
    {
        try
        {
            lock (Gate) File.WriteAllText(System.IO.Path.Combine(DataDir, name), JsonSerializer.Serialize(o, o.GetType()));
        }
        catch { }
    }

    public static void SaveCfg() => Write("settings.json", Cfg);
    public static void SaveProg() => Write("progress.json", Progress);
    public static void SaveDur() => Write("durations.json", Durations);
}

public static class Library
{
    public static readonly HashSet<string> Video = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".ts", ".m2ts", ".3gp", ".ogv", ".vob", ".mts", ".divx", ".rmvb" };
    public static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".m4a", ".aac", ".flac", ".wav", ".ogg", ".opus", ".wma", ".amr", ".mka", ".ape", ".alac" };

    public static bool IsMedia(string p) { var e = System.IO.Path.GetExtension(p); return Video.Contains(e) || Audio.Contains(e); }

    public static MediaItem For(string full)
    {
        var fi = new FileInfo(full);
        var dir = fi.DirectoryName ?? "";
        return new MediaItem
        {
            FullPath = full,
            Title = System.IO.Path.GetFileNameWithoutExtension(full),
            Folder = dir,
            FolderName = string.IsNullOrEmpty(dir) ? "" : (new DirectoryInfo(dir).Name),
            Size = fi.Exists ? fi.Length : 0,
            Mtime = fi.Exists ? fi.LastWriteTime : DateTime.MinValue,
            IsAudio = Audio.Contains(fi.Extension),
        };
    }

    public static List<MediaItem> Scan(IEnumerable<string> folders)
    {
        var map = new Dictionary<string, MediaItem>(StringComparer.OrdinalIgnoreCase);
        var opt = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MaxRecursionDepth = 10 };
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(folder, "*", opt))
                {
                    if (!IsMedia(f) || map.ContainsKey(f)) continue;
                    try { map[f] = For(f); } catch { }
                }
            }
            catch { }
        }
        return map.Values.ToList();
    }
}

/// <summary>Video thumbnails: our own cache, then the Windows shell thumbnail (same as Explorer).</summary>
public static class Thumbs
{
    [StructLayout(LayoutKind.Sequential)] struct SIZE { public int cx, cy; }

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItemImageFactory
    {
        [PreserveSig] int GetImage(SIZE size, int flags, out IntPtr phbm);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    static extern void SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr h);

    static string Key(MediaItem f)
    {
        var h = SHA1.HashData(Encoding.UTF8.GetBytes($"{f.FullPath}|{f.Size}|{f.Mtime.Ticks}"));
        return Convert.ToHexString(h).Substring(0, 24) + ".jpg";
    }

    public static string CachePath(MediaItem f) => System.IO.Path.Combine(Store.ThumbDir, Key(f));

    /// <summary>Call from a background thread.</summary>
    public static BitmapSource Load(MediaItem f)
    {
        if (f.IsAudio || f.IsUrl) return null;
        var cp = CachePath(f);
        if (File.Exists(cp)) { var b = Decode(cp); if (b != null) return b; }
        var s = Shell(f.FullPath, 480, 270);
        if (s != null)
        {
            try
            {
                var conv = new FormatConvertedBitmap(s, PixelFormats.Bgr32, null, 0);
                conv.Freeze();
                var enc = new JpegBitmapEncoder { QualityLevel = 85 };
                enc.Frames.Add(BitmapFrame.Create(conv));
                using var fs = File.Create(cp);
                enc.Save(fs);
                return conv;
            }
            catch { return s; }
        }
        return null;
    }

    static BitmapSource Decode(string path)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 480;
            bi.UriSource = new Uri(path);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    static BitmapSource Shell(string path, int w, int h)
    {
        try
        {
            var iid = typeof(IShellItemImageFactory).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var fac);
            // THUMBNAILONLY (8) | BIGGERSIZEOK (1): fail instead of returning a generic icon
            int hr = fac.GetImage(new SIZE { cx = w, cy = h }, 9, out var hbm);
            if (hr != 0 || hbm == IntPtr.Zero) return null;
            try
            {
                var src = Imaging.CreateBitmapSourceFromHBitmap(hbm, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
            finally { DeleteObject(hbm); }
        }
        catch { return null; }
    }
}

public static class Durations
{
    static readonly SemaphoreSlim Sem = new(1);

    public static async Task<long> Get(MediaItem f)
    {
        if (f.IsUrl) return 0;
        lock (Store.Durations) { if (Store.Durations.TryGetValue(f.FullPath, out var d)) return d; }
        await Sem.WaitAsync();
        try
        {
            lock (Store.Durations) { if (Store.Durations.TryGetValue(f.FullPath, out var d2)) return d2; }
            long ms = 0;
            using (var m = new Media(App.Vlc, f.FullPath, FromType.FromPath))
            {
                await m.Parse(MediaParseOptions.ParseLocal, 8000);
                ms = m.Duration;
            }
            if (ms > 0)
            {
                lock (Store.Durations) Store.Durations[f.FullPath] = ms;
                Store.SaveDur();
            }
            return ms;
        }
        catch { return 0; }
        finally { Sem.Release(); }
    }
}
