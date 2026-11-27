using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using WShapes = System.Windows.Shapes;

namespace Cascade;

public partial class PlayerWindow : Window
{
    public static PlayerWindow Current;

    // ---- native helpers ----
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_POWER_STATUS { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag; public int BatteryLifeTime, BatteryFullLifeTime; }
    [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS s);

    // ---- state ----
    List<MediaItem> _q;
    int _i;
    MediaItem _f;
    MediaPlayer _mp;
    bool _started, _audioMode, _userAudio, _boost, _suppress, _down, _fs, _uiOn = true, _closing, _thumbTried;
    long _resume;
    double _prevRate = 1, _rate = 1;
    int _fit, _sleepMin;
    DateTime _lastSave = DateTime.MinValue, _playStart = DateTime.MaxValue;
    WindowStyle _oldStyle; ResizeMode _oldResize; WindowState _oldState;
    string _panel = "";

    // ---- ui ----
    Grid _ui;
    Border _audioArt, _topBar, _bottom, _gauge, _sysPill, _panelBox, _panelScrim, _boostPill;
    WShapes.Rectangle _shadeTop, _shadeBot;
    TextBlock _title, _sub, _tcur, _trem, _gaugeText, _speedText, _sysText, _artTitle, _panelTitle;
    Bar _seek, _vol;
    Button _playBtn, _prevBtn, _nextBtn, _muteBtn, _audioBtn, _pinBtn;
    StackPanel _panelBody;
    Grid _rippleL, _rippleR;
    TextBlock _rippleLText, _rippleRText;
    TranslateTransform _panelTr;
    DispatcherTimer _tick, _hide, _click, _hold, _gaugeHide, _sleep;

    static readonly string[] AspectNames = { "Default", "16:9", "4:3", "21:9", "1:1" };
    static readonly string[] AspectVals = { null, "16:9", "4:3", "21:9", "1:1" };

    public PlayerWindow(List<MediaItem> q, int index)
    {
        InitializeComponent();
        Current = this;
        _q = q; _i = Math.Clamp(index, 0, q.Count - 1);
        TaskbarItemInfo = new TaskbarItemInfo();
        _mp = new MediaPlayer(App.Vlc);
        BuildOverlay();
        Video.Content = _ui;

        _mp.Playing += (_, _) => Dispatcher.BeginInvoke(new Action(OnPlaying));
        _mp.Paused += (_, _) => Dispatcher.BeginInvoke(new Action(UpdatePlayIcon));
        _mp.Stopped += (_, _) => Dispatcher.BeginInvoke(new Action(UpdatePlayIcon));
        _mp.EndReached += (_, _) => Dispatcher.BeginInvoke(new Action(OnEnded));
        _mp.EncounteredError += (_, _) => Dispatcher.BeginInvoke(new Action(() => Toasts.Show(_ui, "Can't play this file")));

        _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tick.Tick += (_, _) => Tick();
        _tick.Start();
        _hide = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2800) };
        _hide.Tick += (_, _) => { _hide.Stop(); if (_mp.IsPlaying && _panel == "" && !_audioMode) SetUi(false); };
        _click = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
        _click.Tick += (_, _) => { _click.Stop(); Toggle(); };
        _hold = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _hold.Tick += (_, _) => { _hold.Stop(); if (_mp.IsPlaying) StartBoost(); };
        _gaugeHide = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        _gaugeHide.Tick += (_, _) => { _gaugeHide.Stop(); Anim.Fade(_gauge, 0, 200); };
        _sleep = new DispatcherTimer();
        _sleep.Tick += (_, _) => { _sleep.Stop(); _sleepMin = 0; _mp.SetPause(true); Toasts.Show(_ui, "Sleep timer: paused"); };

        SourceInitialized += (_, _) => DarkTitleBar();
        ContentRendered += (_, _) =>
        {
            if (_started) return;
            _started = true;
            Video.MediaPlayer = _mp;
            _mp.Volume = Math.Clamp(Store.Cfg.Volume, 0, 200);
            LoadItem();
            ShowUi();
            _ui.Focus();
        };
        PreviewKeyDown += (_, e) => OnKey(e);
        Closing += (_, _) => Shutdown();
    }

    void DarkTitleBar()
    {
        try
        {
            var h = new WindowInteropHelper(this).Handle;
            int on = 1, black = 0;
            DwmSetWindowAttribute(h, 20, ref on, 4);
            DwmSetWindowAttribute(h, 19, ref on, 4);
            DwmSetWindowAttribute(h, 35, ref black, 4);   // caption colour (Windows 11)
        }
        catch { }
    }

    // =====================================================================
    //  Overlay UI (hosted by VideoView so it can sit on top of the video)
    // =====================================================================
    static Brush A(string hex) => Util.Brush(hex);

    void BuildOverlay()
    {
        _ui = new Grid { Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), Focusable = true };

        // audio-only art
        _audioArt = new Border { Visibility = Visibility.Collapsed, Background = new LinearGradientBrush(Util.C("#2A1A10"), Util.C("#0C0C0E"), 90) };
        var art = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 80) };
        var logo = Ui.Logo(190, true);
        logo.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Util.C("#F08440"), BlurRadius = 70, ShadowDepth = 18, Opacity = 0.45 };
        art.Children.Add(logo);
        _artTitle = new TextBlock { Foreground = Brushes.White, FontSize = 24, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 700, Margin = new Thickness(0, 28, 0, 0) };
        art.Children.Add(_artTitle);
        _audioArt.Child = art;
        _ui.Children.Add(_audioArt);

        // gradient shades
        _shadeTop = new WShapes.Rectangle { Height = 190, VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false, Fill = new LinearGradientBrush(Color.FromArgb(190, 0, 0, 0), Colors.Transparent, 90) };
        _shadeBot = new WShapes.Rectangle { Height = 250, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false, Fill = new LinearGradientBrush(Colors.Transparent, Color.FromArgb(215, 0, 0, 0), 90) };
        _ui.Children.Add(_shadeTop); _ui.Children.Add(_shadeBot);

        // double-click ripples
        var rg = new Grid { IsHitTestVisible = false };
        rg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Star) });
        rg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Star) });
        rg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Star) });
        _rippleL = Ripple(false, out _rippleLText);
        _rippleR = Ripple(true, out _rippleRText);
        Grid.SetColumn(_rippleR, 2);
        rg.Children.Add(_rippleL); rg.Children.Add(_rippleR);
        _ui.Children.Add(rg);

        // gauge + boost + clock pills
        _gaugeText = new TextBlock { Foreground = Brushes.White, FontSize = 22, FontWeight = FontWeights.ExtraBold };
        _gauge = new Border { CornerRadius = new CornerRadius(20), Background = A("#99000000"), Padding = new Thickness(22, 10, 22, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 90, 0, 0), Opacity = 0, IsHitTestVisible = false, Child = _gaugeText };
        _boostPill = new Border { CornerRadius = new CornerRadius(20), Background = A("#99000000"), Padding = new Thickness(16, 7, 16, 7), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 90, 0, 0), Opacity = 0, IsHitTestVisible = false, Child = new TextBlock { Foreground = Brushes.White, FontWeight = FontWeights.Bold, Text = "2x speed" } };
        _sysText = new TextBlock { Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.SemiBold };
        _sysPill = new Border { CornerRadius = new CornerRadius(16), Background = A("#59000000"), Padding = new Thickness(12, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 14, 16, 0), IsHitTestVisible = false, Opacity = 0, Child = _sysText };
        _ui.Children.Add(_gauge); _ui.Children.Add(_boostPill); _ui.Children.Add(_sysPill);

        BuildTopBar();
        BuildBottom();
        BuildPanel();

        _ui.MouseMove += (_, _) => ShowUi();
        _ui.MouseLeftButtonDown += OnDown;
        _ui.MouseLeftButtonUp += OnUp;
        _ui.MouseWheel += (_, e) => { ChangeVolume(e.Delta > 0 ? 5 : -5); };
        _ui.PreviewKeyDown += (_, e) => OnKey(e);
    }

    Grid Ripple(bool right, out TextBlock text)
    {
        var g = new Grid { Opacity = 0 };
        var b = new Border { Background = A("#26FFFFFF"), CornerRadius = right ? new CornerRadius(400, 0, 0, 400) : new CornerRadius(0, 400, 400, 0) };
        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var ic = Icons.Make(right ? "fwd" : "rew", 44, fill: Brushes.White);
        text = new TextBlock { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(ic); sp.Children.Add(text);
        b.Child = sp;
        g.Children.Add(b);
        return g;
    }

    void FlashRipple(bool right, int sec)
    {
        var g = right ? _rippleR : _rippleL;
        (right ? _rippleRText : _rippleLText).Text = (right ? "+" : "-") + sec + " s";
        var a = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(800) };
        a.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(0.15)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(0.7)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
        g.BeginAnimation(OpacityProperty, a);
    }

    Button Glass(string icon, Action click, double size = 40, string tip = null)
    {
        var b = new Button { Style = (Style)Application.Current.FindResource("GlassBtn"), Width = size, Height = size, ToolTip = tip, Content = Icons.Make(icon, size * 0.5, fill: Brushes.White), Background = A("#73000000") };
        b.Click += (_, _) => click();
        return b;
    }

    Button PillIcon(string icon, Action click, string tip)
    {
        var b = new Button { Style = (Style)Application.Current.FindResource("TabBtn"), Width = 42, Height = 42, ToolTip = tip, Content = Icons.Make(icon, 22, fill: Brushes.White) };
        var sc = Anim.Hover(b, 1.12, 0.82);
        b.Click += (_, _) => click();
        return b;
    }

    void BuildTopBar()
    {
        _topBar = new Border { VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(18, 14, 18, 0), RenderTransform = new TranslateTransform(0, -16) };
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var back = Glass("back", Close, 44, "Back (Esc)");
        g.Children.Add(back);
        var tx = new StackPanel { Margin = new Thickness(14, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
        _title = new TextBlock { Foreground = Brushes.White, FontSize = 17, FontWeight = FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
        _sub = new TextBlock { Foreground = A("#A6FFFFFF"), FontSize = 12.5 };
        tx.Children.Add(_title); tx.Children.Add(_sub);
        Grid.SetColumn(tx, 1);
        g.Children.Add(tx);
        var pill = new Border { CornerRadius = new CornerRadius(24), Background = A("#73000000"), BorderBrush = A("#24FFFFFF"), BorderThickness = new Thickness(1), Padding = new Thickness(6, 2, 6, 2), VerticalAlignment = VerticalAlignment.Center };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(PillIcon("camera", Screenshot, "Screenshot (S)"));
        _audioBtn = PillIcon("headphones", () => SetAudioMode(!_audioMode, true), "Play as audio (V)");
        row.Children.Add(_audioBtn);
        _pinBtn = PillIcon("pin", () => { Topmost = !Topmost; Toasts.Show(_ui, Topmost ? "Always on top: on" : "Always on top: off"); }, "Always on top (T)");
        row.Children.Add(_pinBtn);
        row.Children.Add(PillIcon("aspect", CycleAspect, "Aspect ratio (A)"));
        pill.Child = row;
        Grid.SetColumn(pill, 2);
        g.Children.Add(pill);
        _topBar.Child = g;
        _ui.Children.Add(_topBar);
    }

    void BuildBottom()
    {
        _bottom = new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(20, 0, 20, 18), CornerRadius = new CornerRadius(28),
            Background = A("#8C000000"), BorderBrush = A("#24FFFFFF"), BorderThickness = new Thickness(1), Padding = new Thickness(20, 22, 20, 8),
            RenderTransform = new TranslateTransform(0, 24),
        };
        var col = new StackPanel();

        var seekRow = new Grid();
        seekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        seekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        seekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
        _tcur = new TextBlock { Foreground = Brushes.White, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center, Text = "00:00" };
        _trem = new TextBlock { Foreground = A("#CCFFFFFF"), FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Text = "-00:00" };
        _seek = new Bar(34) { Label = f => Util.Fmt((long)(f * Math.Max(0, _mp.Length))) };
        _seek.Dragging += f => _tcur.Text = Util.Fmt((long)(f * Math.Max(0, _mp.Length)));
        _seek.Committed += f => { if (_mp.Length > 0) _mp.Time = (long)(f * _mp.Length); };
        Grid.SetColumn(_seek, 1); Grid.SetColumn(_trem, 2);
        seekRow.Children.Add(_tcur); seekRow.Children.Add(_seek); seekRow.Children.Add(_trem);
        col.Children.Add(seekRow);

        // controls: volume left | transport centre | options right
        var row = new Grid { Margin = new Thickness(0, 2, 0, 0) };

        var left = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        _muteBtn = PillIcon("vol", () => { _mp.Mute = !_mp.Mute; UpdateVolIcon(); }, "Mute (M)");
        _vol = new Bar(30) { Width = 110, Label = f => (int)(f * 200) + "%" };
        _vol.Set(Store.Cfg.Volume / 200.0);
        _vol.Dragging += f => SetVolume((int)Math.Round(f * 200));
        _vol.Committed += f => SetVolume((int)Math.Round(f * 200));
        left.Children.Add(_muteBtn); left.Children.Add(_vol);
        row.Children.Add(left);

        var mid = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _prevBtn = Glass("prev", () => Go(-1), 40, "Previous (P)");
        _playBtn = new Button { Style = (Style)Application.Current.FindResource("GlassBtn"), Width = 62, Height = 62, Margin = new Thickness(12, 0, 12, 0), Background = Ui.BrandGradient(), ToolTip = "Play / pause (Space)" };
        _playBtn.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Util.C("#F08440"), BlurRadius = 20, ShadowDepth = 4, Opacity = 0.5 };
        _playBtn.Click += (_, _) => Toggle();
        _nextBtn = Glass("next", () => Go(1), 40, "Next (N)");
        mid.Children.Add(_prevBtn); mid.Children.Add(_playBtn); mid.Children.Add(_nextBtn);
        row.Children.Add(mid);

        var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var chip = new Border { CornerRadius = new CornerRadius(20), Background = A("#1AFFFFFF"), Padding = new Thickness(12, 7, 12, 7), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0) };
        var cr = new StackPanel { Orientation = Orientation.Horizontal };
        cr.Children.Add(Icons.Make("speed", 16, fill: Brushes.White));
        _speedText = new TextBlock { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(6, 0, 0, 0), Text = "1x" };
        cr.Children.Add(_speedText);
        chip.Child = cr;
        Anim.Hover(chip, 1.06, 0.94);
        chip.MouseLeftButtonUp += (_, _) => OpenPanel("speed");
        right.Children.Add(chip);
        right.Children.Add(PillIcon("subs", () => OpenPanel("subs"), "Subtitles (C)"));
        right.Children.Add(PillIcon("track", () => OpenPanel("audio"), "Audio track"));
        right.Children.Add(PillIcon("more", () => OpenPanel("more"), "More"));
        right.Children.Add(PillIcon("full", ToggleFullscreen, "Fullscreen (F)"));
        row.Children.Add(right);

        col.Children.Add(row);
        _bottom.Child = col;
        _ui.Children.Add(_bottom);
        UpdatePlayIcon();
    }

    void BuildPanel()
    {
        _panelScrim = new Border { Background = A("#66000000"), Opacity = 0, IsHitTestVisible = false };
        _panelScrim.MouseLeftButtonDown += (_, _) => ClosePanel();
        _ui.Children.Add(_panelScrim);

        _panelTr = new TranslateTransform(400, 0);
        _panelBox = new Border
        {
            Width = 360, HorizontalAlignment = HorizontalAlignment.Right, CornerRadius = new CornerRadius(28, 0, 0, 28), Background = A("#F2141519"),
            BorderBrush = A("#1AFFFFFF"), BorderThickness = new Thickness(1), Padding = new Thickness(22), RenderTransform = _panelTr, Visibility = Visibility.Collapsed,
        };
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var head = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        _panelTitle = new TextBlock { Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
        var x = Glass("close", ClosePanel, 36);
        x.HorizontalAlignment = HorizontalAlignment.Right;
        head.Children.Add(_panelTitle); head.Children.Add(x);
        var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Focusable = false };
        _panelBody = new StackPanel();
        sv.Content = _panelBody;
        Grid.SetRow(sv, 1);
        g.Children.Add(head); g.Children.Add(sv);
        _panelBox.Child = g;
        _ui.Children.Add(_panelBox);
    }

    // ---- panel content helpers ----
    TextBlock PLabel(string t) => new TextBlock { Text = t, Foreground = A("#8CFFFFFF"), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 8) };

    Border PRow(string text, bool selected, Action click)
    {
        var b = new Border { CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand, Background = A(selected ? "#38F08440" : "#0FFFFFFF") };
        var g = new Grid();
        g.Children.Add(new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 26, 0) });
        if (selected) g.Children.Add(new TextBlock { Text = "✓", Foreground = A("#F08440"), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right });
        b.Child = g;
        Anim.Hover(b, 1.01, 0.98);
        b.MouseLeftButtonUp += (_, _) => click();
        return b;
    }

    WrapPanel PChips(IEnumerable<(string label, bool sel, Action act)> items)
    {
        var wp = new WrapPanel();
        foreach (var (label, sel, act) in items)
        {
            var b = new Border { CornerRadius = new CornerRadius(18), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand, Background = A(sel ? "#F08440" : "#1AFFFFFF") };
            b.Child = new TextBlock { Text = label, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 13 };
            Anim.Hover(b, 1.06, 0.93);
            var a = act;
            b.MouseLeftButtonUp += (_, _) => { a(); if (_panel != "") OpenPanel(_panel); };
            wp.Children.Add(b);
        }
        return wp;
    }

    void OpenPanel(string kind)
    {
        _panel = kind;
        _panelBody.Children.Clear();
        switch (kind)
        {
            case "speed":
                _panelTitle.Text = "Playback speed";
                _panelBody.Children.Add(new TextBlock { Text = Util.SpeedLabel(_rate), Foreground = A("#F08440"), FontSize = 42, FontWeight = FontWeights.ExtraBold });
                var sl = new Slider { Minimum = 0.25, Maximum = 4, Value = _rate, SmallChange = 0.05, TickFrequency = 0.05, IsSnapToTickEnabled = true, Margin = new Thickness(0, 8, 0, 10) };
                sl.ValueChanged += (_, e) => { SetRate(e.NewValue, false); ((TextBlock)_panelBody.Children[0]).Text = Util.SpeedLabel(_rate); };
                _panelBody.Children.Add(sl);
                _panelBody.Children.Add(PChips(new double[] { 0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4 }.Select(r => (Util.SpeedLabel(r), Math.Abs(_rate - r) < 0.01, (Action)(() => SetRate(r, false))))));
                break;
            case "subs":
                _panelTitle.Text = "Subtitles";
                var spu = SafeTracks(() => _mp.SpuDescription);
                int cur = _mp.Spu;
                if (spu.Count == 0) _panelBody.Children.Add(new TextBlock { Text = "No subtitle tracks in this file.", Foreground = A("#99FFFFFF"), Margin = new Thickness(0, 0, 0, 10) });
                foreach (var t in spu)
                {
                    var id = t.Id;
                    _panelBody.Children.Add(PRow(id == -1 ? "Off" : t.Name, id == cur, () => { _mp.SetSpu(id); ClosePanel(); }));
                }
                _panelBody.Children.Add(PRow("Load subtitle file…", false, () =>
                {
                    ClosePanel();
                    var d = new Microsoft.Win32.OpenFileDialog { Filter = "Subtitles|*.srt;*.ass;*.ssa;*.vtt;*.sub|All files|*.*" };
                    if (d.ShowDialog() == true) { _mp.AddSlave(MediaSlaveType.Subtitle, new Uri(d.FileName).AbsoluteUri, true); Toasts.Show(_ui, "Subtitle loaded"); }
                }));
                break;
            case "audio":
                _panelTitle.Text = "Audio track";
                var at = SafeTracks(() => _mp.AudioTrackDescription);
                int acur = _mp.AudioTrack;
                if (at.Count == 0) _panelBody.Children.Add(new TextBlock { Text = "No audio tracks.", Foreground = A("#99FFFFFF") });
                foreach (var t in at)
                {
                    var id = t.Id;
                    _panelBody.Children.Add(PRow(id == -1 ? "Disable" : t.Name, id == acur, () => { _mp.SetAudioTrack(id); ClosePanel(); }));
                }
                break;
            default:
                _panelTitle.Text = "More";
                _panelBody.Children.Add(PLabel("Repeat"));
                _panelBody.Children.Add(PChips(new[] { ("Off", 0), ("One", 1), ("All", 2) }.Select(o => (o.Item1, Store.Cfg.Repeat == o.Item2, (Action)(() => { Store.Cfg.Repeat = o.Item2; Store.SaveCfg(); })))));
                _panelBody.Children.Add(PLabel("Sleep timer"));
                _panelBody.Children.Add(PChips(new[] { ("Off", 0), ("15 min", 15), ("30 min", 30), ("45 min", 45), ("60 min", 60) }.Select(o => (o.Item1, _sleepMin == o.Item2, (Action)(() => SetSleep(o.Item2))))));
                _panelBody.Children.Add(PLabel("Skip step (double-click sides, arrow keys)"));
                _panelBody.Children.Add(PChips(new[] { 5, 10, 15, 30 }.Select(s => (s + " s", Store.Cfg.Seek == s, (Action)(() => { Store.Cfg.Seek = s; Store.SaveCfg(); })))));
                _panelBody.Children.Add(PLabel("Hold-to-speed (press and hold the video)"));
                _panelBody.Children.Add(PChips(new[] { 15, 20, 30, 40 }.Select(s => (Util.SpeedLabel(s / 10.0), Store.Cfg.HoldSpeedX10 == s, (Action)(() => { Store.Cfg.HoldSpeedX10 = s; Store.SaveCfg(); })))));
                if (!_f.IsUrl) _panelBody.Children.Add(PRow("Show in Explorer", false, () => Process.Start("explorer.exe", $"/select,\"{_f.FullPath}\"")));
                _panelBody.Children.Add(PLabel("Keyboard"));
                _panelBody.Children.Add(new TextBlock
                {
                    Foreground = A("#BFFFFFFF"), FontSize = 12.5, LineHeight = 20, TextWrapping = TextWrapping.Wrap,
                    Text = "Space  play / pause\n← →  skip (Shift = 30 s)\n↑ ↓  volume\nM  mute      F  fullscreen\nN / P  next / previous\nS  screenshot      A  aspect ratio\nC  subtitles      V  audio only\n[ ]  speed      T  always on top\nEsc  close panel / exit fullscreen / close",
                });
                break;
        }
        _panelBox.Visibility = Visibility.Visible;
        _panelScrim.IsHitTestVisible = true;
        Anim.Fade(_panelScrim, 1, 220);
        Anim.To(_panelTr, TranslateTransform.XProperty, 0, 420, Anim.Spring);
        ShowUi();
    }

    static List<TrackDescription> SafeTracks(Func<TrackDescription[]> get)
    {
        try { return (get() ?? Array.Empty<TrackDescription>()).ToList(); } catch { return new List<TrackDescription>(); }
    }

    void ClosePanel()
    {
        if (_panel == "") return;
        _panel = "";
        _panelScrim.IsHitTestVisible = false;
        Anim.Fade(_panelScrim, 0, 200);
        var a = new DoubleAnimation(400, TimeSpan.FromMilliseconds(260)) { EasingFunction = Anim.Smooth };
        a.Completed += (_, _) => { if (_panel == "") _panelBox.Visibility = Visibility.Collapsed; };
        _panelTr.BeginAnimation(TranslateTransform.XProperty, a);
        ShowUi();
    }

    // =====================================================================
    //  Playback
    // =====================================================================
    public void ReplaceQueue(List<MediaItem> list, int index)
    {
        SaveProgress();
        _q = list; _i = Math.Clamp(index, 0, list.Count - 1);
        LoadItem();
        Activate();
    }

    void LoadItem()
    {
        _f = _q[_i];
        _thumbTried = false;
        _playStart = DateTime.MaxValue;
        Title = _f.Title + " – Cascade";
        _title.Text = _artTitle.Text = _f.Title;
        Store.Progress.TryGetValue(_f.FullPath, out var p);
        _resume = p != null && p.Pos > 5000 && (p.Dur <= 0 || p.Pos < p.Dur * 0.97) ? p.Pos : 0;
        SetAudioMode(_f.IsAudio || _userAudio, false);
        _prevBtn.Visibility = _nextBtn.Visibility = _q.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        UpdateSub();
        using var media = _f.IsUrl ? new Media(App.Vlc, new Uri(_f.FullPath)) : new Media(App.Vlc, _f.FullPath, FromType.FromPath);
        _mp.Play(media);
    }

    void OnPlaying()
    {
        UpdatePlayIcon();
        if (_playStart == DateTime.MaxValue) _playStart = DateTime.Now;
        if (_rate != 1) _mp.SetRate((float)_rate);
        // pick up subtitle files that sit next to the video (same name)
        if (!_f.IsUrl)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_f.FullPath);
                var baseName = System.IO.Path.GetFileNameWithoutExtension(_f.FullPath);
                foreach (var s in Directory.EnumerateFiles(dir, baseName + "*").Where(x => new[] { ".srt", ".ass", ".ssa", ".vtt", ".sub" }.Contains(System.IO.Path.GetExtension(x).ToLowerInvariant())).Take(1))
                    _mp.AddSlave(MediaSlaveType.Subtitle, new Uri(s).AbsoluteUri, true);
            }
            catch { }
        }
    }

    void OnEnded()
    {
        SaveProgress(true);
        if (Store.Cfg.Repeat == 1) { LoadItem(); return; }
        if (_i < _q.Count - 1) { _i++; LoadItem(); }
        else if (Store.Cfg.Repeat == 2) { _i = 0; LoadItem(); }
        else { UpdatePlayIcon(); SetUi(true); }
    }

    void Go(int d)
    {
        int n = _i + d;
        if (n < 0 || n >= _q.Count) return;
        SaveProgress();
        _i = n;
        LoadItem();
    }

    void Toggle()
    {
        var st = _mp.State;
        if (st == VLCState.Ended || st == VLCState.Stopped || st == VLCState.Error) { LoadItem(); return; }
        _mp.SetPause(_mp.IsPlaying);
        UpdatePlayIcon();
        ShowUi();
    }

    void UpdatePlayIcon()
    {
        if (_playBtn == null) return;
        _playBtn.Content = Icons.Make(_mp != null && _mp.IsPlaying ? "pause" : "play", 32, fill: Brushes.White);
    }

    void SkipBy(int sec, bool ripple = true)
    {
        if (_mp.Length <= 0) return;
        _mp.Time = Math.Clamp(_mp.Time + sec * 1000L, 0, _mp.Length);
        if (ripple) FlashRipple(sec > 0, Math.Abs(sec));
    }

    void SetRate(double r, bool toast)
    {
        _rate = Math.Round(Math.Clamp(r, 0.25, 4), 2);
        _mp.SetRate((float)_rate);
        _speedText.Text = Util.SpeedLabel(_rate);
        if (toast) Gauge("Speed " + Util.SpeedLabel(_rate));
    }

    void SetVolume(int v)
    {
        v = Math.Clamp(v, 0, 200);
        _mp.Mute = false;
        _mp.Volume = v;
        Store.Cfg.Volume = v;
        _vol.Set(v / 200.0);
        UpdateVolIcon();
    }

    void ChangeVolume(int d)
    {
        SetVolume(_mp.Volume + d);
        Gauge("Volume " + _mp.Volume + "%");
    }

    void UpdateVolIcon()
    {
        if (_muteBtn != null) _muteBtn.Content = Icons.Make(_mp.Mute || _mp.Volume == 0 ? "mute" : "vol", 22, fill: Brushes.White);
    }

    void CycleAspect()
    {
        _fit = (_fit + 1) % AspectVals.Length;
        _mp.AspectRatio = AspectVals[_fit];
        Gauge("Aspect: " + AspectNames[_fit]);
    }

    void SetSleep(int min)
    {
        _sleep.Stop();
        _sleepMin = min;
        if (min > 0) { _sleep.Interval = TimeSpan.FromMinutes(min); _sleep.Start(); Toasts.Show(_ui, $"Pausing in {min} min"); }
    }

    void SetAudioMode(bool on, bool toast)
    {
        if (toast && _f.IsAudio) { Toasts.Show(_ui, "This is an audio file"); return; }
        if (toast) _userAudio = on;
        _audioMode = on;
        _audioArt.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on) SetUi(true);
        if (toast) Toasts.Show(_ui, on ? "Audio only" : "Video on");
    }

    void Screenshot()
    {
        try
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Cascade");
            Directory.CreateDirectory(dir);
            var file = System.IO.Path.Combine(dir, $"Cascade_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            if (_mp.TakeSnapshot(0, file, 0, 0)) Toasts.Show(_ui, "Screenshot saved to Pictures\\Cascade", "Show", () => Process.Start("explorer.exe", $"/select,\"{file}\""));
            else Toasts.Show(_ui, "Couldn't take screenshot");
        }
        catch { Toasts.Show(_ui, "Couldn't take screenshot"); }
    }

    void ToggleFullscreen()
    {
        if (!_fs)
        {
            _oldStyle = WindowStyle; _oldResize = ResizeMode; _oldState = WindowState;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _fs = true;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = _oldStyle; ResizeMode = _oldResize;
            WindowState = _oldState;
            _fs = false;
        }
        _ui.Focus();
    }

    // ---- periodic update ----
    void Tick()
    {
        if (_mp == null || _closing) return;
        long len = _mp.Length, t = _mp.Time;
        if (_resume > 0 && len > 0)
        {
            _mp.Time = _resume;
            Toasts.Show(_ui, "Resumed from " + Util.Fmt(_resume));
            _resume = 0;
        }
        if (len > 0)
        {
            if (!_seek.IsDragging) { _seek.Set((double)t / len); _tcur.Text = Util.Fmt(t); }
            _trem.Text = "-" + Util.Fmt(len - t);
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            TaskbarItemInfo.ProgressValue = Math.Clamp((double)t / len, 0, 1);
        }
        if ((DateTime.Now - _lastSave).TotalSeconds > 3 && _mp.IsPlaying) SaveProgress();
        if (!_thumbTried && !_f.IsAudio && !_f.IsUrl && _mp.IsPlaying && (DateTime.Now - _playStart).TotalSeconds > 3)
        {
            _thumbTried = true;
            try { var cp = Thumbs.CachePath(_f); if (!File.Exists(cp)) _mp.TakeSnapshot(0, cp, 480, 0); } catch { }
        }
        UpdateSub();
    }

    void SaveProgress(bool finished = false)
    {
        if (_f == null || _mp == null) return;
        long len = _mp.Length;
        if (len <= 0) return;
        _lastSave = DateTime.Now;
        Store.Progress[_f.FullPath] = new Prog { Pos = finished ? 0 : _mp.Time, Dur = len, T = DateTimeOffset.Now.ToUnixTimeMilliseconds() };
        Store.SaveProg();
    }

    string SysInfo()
    {
        string time = DateTime.Now.ToString("h:mm tt", System.Globalization.CultureInfo.CurrentCulture).ToLowerInvariant();
        try
        {
            if (GetSystemPowerStatus(out var s) && s.BatteryLifePercent <= 100 && (s.BatteryFlag & 128) == 0)
                return (s.ACLineStatus == 1 ? "⚡" : "") + s.BatteryLifePercent + "%  " + time;
        }
        catch { }
        return time;
    }

    void UpdateSub()
    {
        var parts = new List<string>();
        if (_q.Count > 1) parts.Add($"{_i + 1} of {_q.Count}");
        var info = SysInfo();
        parts.Add(info);
        _sub.Text = string.Join("  ·  ", parts);
        _sysText.Text = info;
        _speedText.Text = Util.SpeedLabel(_rate);
    }

    // =====================================================================
    //  UI visibility + input
    // =====================================================================
    void ShowUi() { SetUi(true); _hide.Stop(); if (_mp.IsPlaying && _panel == "" && !_audioMode) _hide.Start(); }

    void SetUi(bool on)
    {
        _uiOn = on;
        _ui.Cursor = on ? Cursors.Arrow : Cursors.None;
        Anim.Fade(_shadeTop, on ? 1 : 0, 280);
        Anim.Fade(_shadeBot, on ? 1 : 0, 280);
        Anim.Fade(_sysPill, on ? 0 : 1, 280);
        FadeSlide(_topBar, on, -16);
        FadeSlide(_bottom, on, 24);
    }

    static void FadeSlide(Border b, bool on, double offset)
    {
        b.IsHitTestVisible = on;
        Anim.Fade(b, on ? 1 : 0, on ? 260 : 200);
        if (b.RenderTransform is TranslateTransform tr) Anim.To(tr, TranslateTransform.YProperty, on ? 0 : offset, on ? 420 : 220, on ? Anim.Spring : Anim.Smooth);
    }

    bool InControls(object src)
    {
        var d = src as DependencyObject;
        while (d != null && !ReferenceEquals(d, _ui))
        {
            if (ReferenceEquals(d, _topBar) || ReferenceEquals(d, _bottom) || ReferenceEquals(d, _panelBox) || ReferenceEquals(d, _panelScrim)) return true;
            d = d is Visual || d is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    void OnDown(object s, MouseButtonEventArgs e)
    {
        _ui.Focus();
        if (InControls(e.OriginalSource)) return;
        _down = true;
        if (e.ClickCount >= 2)
        {
            _click.Stop(); _hold.Stop(); _suppress = true;
            double x = e.GetPosition(_ui).X / Math.Max(1, _ui.ActualWidth);
            if (x < 0.34) SkipBy(-Store.Cfg.Seek);
            else if (x > 0.66) SkipBy(Store.Cfg.Seek);
            else ToggleFullscreen();
            return;
        }
        _suppress = false;
        _hold.Start();
    }

    void OnUp(object s, MouseButtonEventArgs e)
    {
        _hold.Stop();
        if (_boost) { EndBoost(); _down = false; return; }
        if (!_down) return;
        _down = false;
        if (_suppress) { _suppress = false; return; }
        if (InControls(e.OriginalSource)) return;
        _click.Stop(); _click.Start();     // single click = play / pause (after the double-click window)
    }

    void StartBoost()
    {
        _boost = true; _suppress = true;
        _prevRate = _mp.Rate;
        double r = Store.Cfg.HoldSpeedX10 / 10.0;
        _mp.SetRate((float)r);
        ((TextBlock)_boostPill.Child).Text = Util.SpeedLabel(r) + " speed";
        Anim.Fade(_boostPill, 1, 150);
    }

    void EndBoost()
    {
        _boost = false;
        _mp.SetRate((float)_prevRate);
        Anim.Fade(_boostPill, 0, 150);
    }

    void Gauge(string text)
    {
        _gaugeText.Text = text;
        Anim.Fade(_gauge, 1, 120);
        _gaugeHide.Stop(); _gaugeHide.Start();
    }

    void OnKey(KeyEventArgs e)
    {
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        switch (e.Key)
        {
            case Key.Space: case Key.K: Toggle(); break;
            case Key.Left: SkipBy(shift ? -30 : -Store.Cfg.Seek); break;
            case Key.Right: SkipBy(shift ? 30 : Store.Cfg.Seek); break;
            case Key.Up: ChangeVolume(5); break;
            case Key.Down: ChangeVolume(-5); break;
            case Key.M: _mp.Mute = !_mp.Mute; UpdateVolIcon(); Gauge(_mp.Mute ? "Muted" : "Volume " + _mp.Volume + "%"); break;
            case Key.F: case Key.F11: ToggleFullscreen(); break;
            case Key.N: Go(1); break;
            case Key.P: Go(-1); break;
            case Key.S: Screenshot(); break;
            case Key.A: CycleAspect(); break;
            case Key.V: SetAudioMode(!_audioMode, true); break;
            case Key.T: Topmost = !Topmost; Toasts.Show(_ui, Topmost ? "Always on top: on" : "Always on top: off"); break;
            case Key.C:
                {
                    var spu = SafeTracks(() => _mp.SpuDescription);
                    if (spu.Count > 0) { int i = spu.FindIndex(t => t.Id == _mp.Spu); var nx = spu[(i + 1) % spu.Count]; _mp.SetSpu(nx.Id); Gauge(nx.Id == -1 ? "Subtitles off" : nx.Name); }
                    break;
                }
            case Key.OemOpenBrackets: SetRate(_rate - 0.1, true); break;
            case Key.OemCloseBrackets: SetRate(_rate + 0.1, true); break;
            case Key.Escape:
                if (_panel != "") ClosePanel();
                else if (_fs) ToggleFullscreen();
                else Close();
                break;
            default: return;
        }
        e.Handled = true;
    }

    // =====================================================================
    //  Shutdown
    // =====================================================================
    void Shutdown()
    {
        if (_closing) return;
        _closing = true;
        SaveProgress();
        foreach (var t in new[] { _tick, _hide, _click, _hold, _gaugeHide, _sleep }) t?.Stop();
        Current = null;
        var mp = _mp;
        try { Video.MediaPlayer = null; } catch { }
        try { Video.Dispose(); } catch { }
        Task.Run(() =>
        {
            try { mp.Stop(); } catch { }
            try { mp.Dispose(); } catch { }
        });
    }
}
