using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using WShapes = System.Windows.Shapes;

namespace Cascade;

public static class ScrollAnim
{
    public static readonly DependencyProperty HOffsetProperty = DependencyProperty.RegisterAttached(
        "HOffset", typeof(double), typeof(ScrollAnim),
        new PropertyMetadata(0.0, (d, e) => { if (d is ScrollViewer sv) sv.ScrollToHorizontalOffset((double)e.NewValue); }));

    public static void SetHOffset(DependencyObject o, double v) => o.SetValue(HOffsetProperty, v);
    public static double GetHOffset(DependencyObject o) => (double)o.GetValue(HOffsetProperty);

    public static void To(ScrollViewer sv, double target)
    {
        var a = new DoubleAnimation(sv.HorizontalOffset, target, TimeSpan.FromMilliseconds(450)) { EasingFunction = Anim.Smooth };
        sv.BeginAnimation(HOffsetProperty, a);
    }
}

public partial class MainWindow : Window
{
    // ---- state ----
    List<MediaItem> _files = new();
    string _tab = "folders", _folder, _query = "";
    List<MediaItem> _view = new(), _hero = new();
    List<(string path, string name, List<MediaItem> items)> _folders = new();
    readonly Dictionary<string, BitmapSource> _thumbMem = new();
    readonly SemaphoreSlim _thumbSem = new(3);
    int _tabIdx, _heroIdx;
    MediaItem _last;
    bool _bdFlip;

    // ---- ui ----
    Grid _root;
    StackPanel _content;
    ScrollViewer _scroll;
    Image _bd1, _bd2;
    WShapes.Rectangle _scrim;
    Border _ind, _fab;
    TextBlock _count;
    TextBox _search;
    Button _maxBtn;
    readonly TextBlock[] _tabText = new TextBlock[3];
    DispatcherTimer _resizeTimer, _searchTimer;
    Button _themeBtn;

    // elastic tab indicator: the two edges animate with different timing, so the pill stretches
    public static readonly DependencyProperty IndLeftProperty = DependencyProperty.Register(nameof(IndLeft), typeof(double), typeof(MainWindow),
        new PropertyMetadata(3.0, (d, _) => ((MainWindow)d).ApplyInd()));
    public static readonly DependencyProperty IndRightProperty = DependencyProperty.Register(nameof(IndRight), typeof(double), typeof(MainWindow),
        new PropertyMetadata(95.0, (d, _) => ((MainWindow)d).ApplyInd()));
    public double IndLeft { get => (double)GetValue(IndLeftProperty); set => SetValue(IndLeftProperty, value); }
    public double IndRight { get => (double)GetValue(IndRightProperty); set => SetValue(IndRightProperty, value); }

    public MainWindow()
    {
        InitializeComponent();
        Build();
        StateChanged += (_, _) => OnState();
        _resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _resizeTimer.Tick += (_, _) => { _resizeTimer.Stop(); if (IsVisible) Render(); };
        SizeChanged += (_, e) => { if (e.WidthChanged) { _resizeTimer.Stop(); _resizeTimer.Start(); } };
        Drop += (_, e) =>
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) HandleArgs(paths);
        };
        Closed += (_, _) => Application.Current.Shutdown();
        Loaded += (_, _) => MoveInd(_tabIdx, false);
    }

    public async void Initialize()
    {
        _files = await Task.Run(() => Library.Scan(Store.Cfg.Folders));
        Render();
    }

    double BackdropOpacity => Theme.Dark ? 0.6 : 0.45;

    // =====================================================================
    //  Layout
    // =====================================================================
    void Build()
    {
        _root = Root;

        var bd = new Grid { IsHitTestVisible = false, ClipToBounds = true };
        _bd1 = NewBackdrop(); _bd2 = NewBackdrop();
        var glow = new WShapes.Rectangle
        {
            Fill = new RadialGradientBrush(new GradientStopCollection { new GradientStop(Color.FromArgb(70, 240, 132, 64), 0), new GradientStop(Colors.Transparent, 1) })
            { Center = new Point(0.05, 0), GradientOrigin = new Point(0.05, 0), RadiusX = 0.7, RadiusY = 0.7 },
        };
        _scrim = new WShapes.Rectangle();
        bd.Children.Add(_bd1); bd.Children.Add(_bd2); bd.Children.Add(glow); bd.Children.Add(_scrim);
        _root.Children.Add(bd);
        ApplyBackdropTheme();

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = BuildHeader();
        Grid.SetRow(header, 0);
        _scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, PanningMode = PanningMode.VerticalOnly, Focusable = false };
        _content = new StackPanel { MaxWidth = 1500, Margin = new Thickness(28, 18, 28, 120) };
        _scroll.Content = _content;
        Grid.SetRow(_scroll, 1);
        layout.Children.Add(_scroll);
        layout.Children.Add(header);
        _root.Children.Add(layout);

        // resume button
        _fab = new Border
        {
            Width = 64, Height = 64, CornerRadius = new CornerRadius(32), Background = Ui.BrandGradient(), Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 34, 34),
            Effect = new DropShadowEffect { Color = Util.C("#F08440"), BlurRadius = 26, ShadowDepth = 6, Opacity = 0.5 },
            Child = Icons.Make("play", 34, fill: Brushes.White), Visibility = Visibility.Collapsed, ToolTip = "Play last played",
        };
        Anim.Hover(_fab, 1.1, 0.88);
        _fab.MouseLeftButtonUp += (_, _) => PlayLast();
        _root.Children.Add(_fab);
    }

    Image NewBackdrop()
    {
        var i = new Image { Stretch = Stretch.UniformToFill, Opacity = 0 };
        RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.HighQuality);
        return i;
    }

    void ApplyBackdropTheme()
    {
        var bg = (Color)Application.Current.Resources["BgColor"];
        _scrim.Fill = new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(40, bg.R, bg.G, bg.B), 0),
            new GradientStop(Color.FromArgb(205, bg.R, bg.G, bg.B), 0.5),
            new GradientStop(bg, 1),
        }, 90);
    }

    void SetBackdrop(BitmapSource src)
    {
        if (src == null || src.PixelWidth == 0) return;
        double k = 48.0 / src.PixelWidth;
        var small = new TransformedBitmap(src, new ScaleTransform(k, k));
        small.Freeze();
        var on = _bdFlip ? _bd1 : _bd2;
        var off = _bdFlip ? _bd2 : _bd1;
        _bdFlip = !_bdFlip;
        on.Source = small;
        Anim.Fade(on, BackdropOpacity, 800);
        Anim.Fade(off, 0, 800);
    }

    Grid BuildHeader()
    {
        var h = new Grid { Height = 60 };
        h.SetResourceReference(Panel.BackgroundProperty, "TopBar");
        for (int i = 0; i < 5; i++) h.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        // brand
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(22, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var logo = Ui.Logo(36);
        logo.RenderTransformOrigin = new Point(0.5, 0.5);
        var pop = new ScaleTransform(0.5, 0.5);
        logo.RenderTransform = pop;
        Loaded += (_, _) => Anim.Scale(pop, 1, 700);
        var col = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        col.Children.Add(Ui.Text("Cascade", 19, FontWeights.ExtraBold));
        _count = Ui.Text("Loading…", 11.5, null, "Muted");
        col.Children.Add(_count);
        brand.Children.Add(logo); brand.Children.Add(col);
        Grid.SetColumn(brand, 0);
        h.Children.Add(brand);

        // search
        var sb = new Border { Height = 38, CornerRadius = new CornerRadius(19), BorderThickness = new Thickness(1), Margin = new Thickness(22, 0, 22, 0), MaxWidth = 420, VerticalAlignment = VerticalAlignment.Center };
        sb.SetResourceReference(Border.BackgroundProperty, "Glass");
        sb.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        var sg = new Grid();
        sg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        sg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var si = Icons.Make("search", 20, "Muted");
        sg.Children.Add(si);
        _search = new TextBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14, Margin = new Thickness(0, 0, 12, 0) };
        _search.SetResourceReference(TextBox.ForegroundProperty, "Fg");
        _search.SetResourceReference(TextBox.CaretBrushProperty, "Fg");
        var ph = Ui.Text("Search your library", 14, null, "Muted");
        ph.IsHitTestVisible = false; ph.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_search, 1); Grid.SetColumn(ph, 1);
        sg.Children.Add(_search); sg.Children.Add(ph);
        sb.Child = sg;
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); _query = _search.Text.Trim(); Render(); };
        _search.TextChanged += (_, _) => { ph.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; _searchTimer.Stop(); _searchTimer.Start(); };
        WindowChrome.SetIsHitTestVisibleInChrome(sb, true);
        Grid.SetColumn(sb, 1);
        h.Children.Add(sb);

        // tabs with elastic indicator
        var tabs = new Border { Width = 282, Height = 40, CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 22, 0) };
        tabs.SetResourceReference(Border.BackgroundProperty, "Glass");
        tabs.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        var tg = new Grid();
        _ind = new Border { HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(17), Background = new LinearGradientBrush(Util.C("#F69A5C"), Util.C("#F08440"), 0) };
        tg.Children.Add(_ind);
        string[] names = { "Videos", "Folders", "Audio" };
        string[] keys = { "videos", "folders", "audio" };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var t = new TextBlock { Text = names[i], FontWeight = FontWeights.SemiBold, FontSize = 14 };
            _tabText[i] = t;
            var b = new Button { Style = (Style)FindResource("TabBtn"), Width = 92, Margin = new Thickness(3 + i * 92, 3, 0, 3), HorizontalAlignment = HorizontalAlignment.Left, Content = t };
            b.Click += (_, _) => SelectTab(keys[idx], idx);
            tg.Children.Add(b);
        }
        tabs.Child = tg;
        WindowChrome.SetIsHitTestVisibleInChrome(tabs, true);
        Grid.SetColumn(tabs, 2);
        h.Children.Add(tabs);
        _tabIdx = Array.IndexOf(keys, _tab);
        IndLeft = 3 + _tabIdx * 92; IndRight = IndLeft + 92;
        UpdateTabColors();

        // actions
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Button addBtn = null, sortBtn = null;
        addBtn = Ui.IconButton("add", () => Menus.Show(addBtn, new (string, Action, bool)[]
        {
            ("Add folder to library…", AddFolderDialog, false),
            ("Manage library folders", ManageFolders, false),
            ("Open file(s)…", OpenFiles, false),
            ("Play network URL…", OpenUrl, false),
        }), 40, "Add");
        sortBtn = Ui.IconButton("sort", () => Menus.Show(sortBtn, new[] { ("Newest", "new"), ("Name", "name"), ("Duration", "dur"), ("Size", "size") }
            .Select(o => (o.Item1, (Action)(() => { Store.Cfg.Sort = o.Item2; Store.SaveCfg(); Render(); }), Store.Cfg.Sort == o.Item2))), 40, "Sort");
        _themeBtn = Ui.IconButton(ThemeIcon(), CycleTheme, 40, "Theme");
        foreach (var b in new[] { addBtn, sortBtn, _themeBtn })
        {
            b.Margin = new Thickness(0, 0, 8, 0);
            WindowChrome.SetIsHitTestVisibleInChrome(b, true);
            actions.Children.Add(b);
        }
        Grid.SetColumn(actions, 3);
        h.Children.Add(actions);

        // caption buttons
        var caps = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        var min = Cap("M0,5 L10,5", () => WindowState = WindowState.Minimized, false);
        _maxBtn = Cap("M0.5,0.5 L9.5,0.5 L9.5,9.5 L0.5,9.5 Z", () => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized, false);
        var close = Cap("M0,0 L10,10 M10,0 L0,10", Close, true);
        caps.Children.Add(min); caps.Children.Add(_maxBtn); caps.Children.Add(close);
        Grid.SetColumn(caps, 4);
        h.Children.Add(caps);
        return h;
    }

    Button Cap(string data, Action click, bool close)
    {
        var b = new Button { Style = (Style)FindResource(close ? "CloseBtn" : "CaptionBtn"), Content = CapIcon(data) };
        b.Click += (_, _) => click();
        WindowChrome.SetIsHitTestVisibleInChrome(b, true);
        return b;
    }

    static WShapes.Path CapIcon(string data)
    {
        var p = new WShapes.Path { Data = Geometry.Parse(data), StrokeThickness = 1, Width = 10, Height = 10, Stretch = Stretch.None, IsHitTestVisible = false };
        p.SetResourceReference(WShapes.Shape.StrokeProperty, "Fg");
        return p;
    }

    void OnState()
    {
        bool max = WindowState == WindowState.Maximized;
        _root.Margin = max ? new Thickness(8) : new Thickness(0);
        _maxBtn.Content = CapIcon(max ? "M2.5,0.5 L9.5,0.5 L9.5,7.5 M0.5,2.5 L7.5,2.5 L7.5,9.5 L0.5,9.5 Z" : "M0.5,0.5 L9.5,0.5 L9.5,9.5 L0.5,9.5 Z");
    }

    // ---- tabs ----
    void ApplyInd()
    {
        if (_ind == null) return;
        _ind.Margin = new Thickness(IndLeft, 3, 0, 3);
        _ind.Width = Math.Max(0, IndRight - IndLeft);
    }

    void MoveInd(int idx, bool animate)
    {
        double l = 3 + idx * 92, r = l + 92;
        bool right = idx > _tabIdx;
        _tabIdx = idx;
        if (!animate) { BeginAnimation(IndLeftProperty, null); BeginAnimation(IndRightProperty, null); IndLeft = l; IndRight = r; return; }
        var fast = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.9 };
        var slow = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
        BeginAnimation(IndLeftProperty, new DoubleAnimation(l, TimeSpan.FromMilliseconds(right ? 620 : 280)) { EasingFunction = right ? slow : fast });
        BeginAnimation(IndRightProperty, new DoubleAnimation(r, TimeSpan.FromMilliseconds(right ? 280 : 620)) { EasingFunction = right ? fast : slow });
    }

    void SelectTab(string key, int idx)
    {
        if (_tab == key && _folder == null) return;
        _tab = key; _folder = null;
        MoveInd(idx, true);
        UpdateTabColors();
        Render();
        _scroll.ScrollToTop();
    }

    void UpdateTabColors()
    {
        string[] keys = { "videos", "folders", "audio" };
        for (int i = 0; i < 3; i++)
        {
            if (keys[i] == _tab) _tabText[i].Foreground = Brushes.White;
            else _tabText[i].SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        }
    }

    string ThemeIcon() => Store.Cfg.Theme == "light" ? "sun" : Store.Cfg.Theme == "dark" ? "moon" : "auto";

    void CycleTheme()
    {
        Store.Cfg.Theme = Store.Cfg.Theme switch { "system" => "light", "light" => "dark", _ => "system" };
        Store.SaveCfg();
        Theme.Apply();
        ApplyBackdropTheme();
        _themeBtn.Content = Icons.Make(ThemeIcon(), 20);
        Toasts.Show(_root, "Theme: " + Store.Cfg.Theme);
        Render();
    }

    // =====================================================================
    //  Rendering
    // =====================================================================
    bool Matches(MediaItem f) => _query.Length == 0 || f.Title.Contains(_query, StringComparison.CurrentCultureIgnoreCase);

    List<MediaItem> Sorted(IEnumerable<MediaItem> src)
    {
        var l = src.ToList();
        switch (Store.Cfg.Sort)
        {
            case "name": l.Sort((a, b) => Util.NatCmp(a.Title, b.Title)); break;
            case "dur": l = l.OrderByDescending(f => { lock (Store.Durations) return Store.Durations.TryGetValue(f.FullPath, out var d) ? d : 0; }).ToList(); break;
            case "size": l = l.OrderByDescending(f => f.Size).ToList(); break;
            default: l = l.OrderByDescending(f => f.Mtime).ToList(); break;
        }
        return l;
    }

    void Render()
    {
        if (_content == null) return;
        _content.Children.Clear();
        double avail = Math.Max(320, Math.Min(1500, ActualWidth > 0 ? ActualWidth : 1280) - 56);
        int nv = _files.Count(f => !f.IsAudio);
        _count.Text = $"{nv} videos · {_files.Count - nv} audio";
        _hero = new List<MediaItem>();
        if (_tab == "videos") RenderVideos(avail);
        else if (_tab == "folders") RenderFolders(avail);
        else RenderAudio();
        _last = ComputeLast();
        _fab.Visibility = _last != null ? Visibility.Visible : Visibility.Collapsed;
    }

    void RenderVideos(double avail)
    {
        var vids = Sorted(_files.Where(f => !f.IsAudio && Matches(f)));
        _view = vids;
        if (_query.Length == 0 && vids.Count > 0) { _hero = HeroList(vids); _content.Children.Add(BuildHero(avail)); }
        _content.Children.Add(Section("All videos", vids.Count));
        if (vids.Count == 0) { _content.Children.Add(Empty("No videos yet. Use + to add a folder.")); return; }
        _content.Children.Add(BuildGrid(vids, avail));
    }

    void RenderFolders(double avail)
    {
        if (_folder == null)
        {
            _folders = _files.Where(f => !f.IsAudio).GroupBy(f => f.Folder)
                .Select(g => (path: g.Key, name: g.First().FolderName, items: g.ToList()))
                .Where(x => _query.Length == 0 || x.name.Contains(_query, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(x => x.name, Comparer<string>.Create(Util.NatCmp)).ToList();
            _content.Children.Add(Section("Folders", _folders.Count, "Library folders", ManageFolders));
            if (_folders.Count == 0) { _content.Children.Add(Empty("No folders found.")); return; }
            var now = DateTime.Now;
            for (int i = 0; i < _folders.Count; i++)
            {
                var f = _folders[i];
                int fresh = f.items.Count(x => (now - x.Mtime).TotalDays < 3);
                _content.Children.Add(FolderRow(f.name, f.path, f.items.Count, fresh, i, () => { _folder = f.path; Render(); _scroll.ScrollToTop(); }));
            }
        }
        else
        {
            var items = Sorted(_files.Where(f => !f.IsAudio && f.Folder == _folder && Matches(f)));
            _view = items;
            var back = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            var bb = Ui.IconButton("back", () => { _folder = null; Render(); }, 42, "Back");
            back.Children.Add(bb);
            var t = Ui.Text(new DirectoryInfo(_folder).Name, 22, FontWeights.ExtraBold);
            t.Margin = new Thickness(14, 0, 0, 0); t.VerticalAlignment = VerticalAlignment.Center;
            back.Children.Add(t);
            _content.Children.Add(back);
            _content.Children.Add(Section("Videos", items.Count));
            if (items.Count == 0) _content.Children.Add(Empty("No videos here."));
            else _content.Children.Add(BuildGrid(items, avail));
        }
    }

    void RenderAudio()
    {
        var aud = Sorted(_files.Where(f => f.IsAudio && Matches(f)));
        _view = aud;
        _content.Children.Add(Section("All audio", aud.Count));
        if (aud.Count == 0) { _content.Children.Add(Empty("No audio found.")); return; }
        for (int i = 0; i < aud.Count; i++)
        {
            int idx = i;
            _content.Children.Add(AudioRow(aud[i], i, () => Play(aud, idx)));
        }
    }

    UIElement Section(string title, int count, string link = null, Action linkAct = null)
    {
        var g = new Grid { Margin = new Thickness(0, 26, 0, 14) };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(Ui.Text(title, 19, FontWeights.Bold));
        var pill = new Border { CornerRadius = new CornerRadius(10), Background = Util.Brush("#26F08440"), Padding = new Thickness(9, 2, 9, 2), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        pill.Child = new TextBlock { Text = count.ToString(), FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Util.Brush("#F08440") };
        sp.Children.Add(pill);
        g.Children.Add(sp);
        if (link != null)
        {
            var l = new TextBlock { Text = link, Foreground = Util.Brush("#F08440"), FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            l.MouseLeftButtonUp += (_, _) => linkAct();
            g.Children.Add(l);
        }
        return g;
    }

    UIElement Empty(string msg)
    {
        var t = Ui.Text(msg, 14, null, "Muted");
        t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 90, 0, 0);
        return t;
    }

    // ---- hero carousel ----
    List<MediaItem> HeroList(List<MediaItem> vids)
    {
        var byPath = vids.ToDictionary(f => f.FullPath, f => f, StringComparer.OrdinalIgnoreCase);
        var recent = Store.Progress.Where(p => byPath.ContainsKey(p.Key) && p.Value.Pos > 5000 && (p.Value.Dur <= 0 || p.Value.Pos < p.Value.Dur * 0.97))
            .OrderByDescending(p => p.Value.T).Select(p => byPath[p.Key]);
        var res = new List<MediaItem>();
        foreach (var f in recent.Concat(vids.OrderByDescending(x => x.Mtime)))
        {
            if (!res.Contains(f)) res.Add(f);
            if (res.Count >= 6) break;
        }
        return res;
    }

    UIElement BuildHero(double avail)
    {
        double cw = Math.Min(640, avail * 0.82), ch = cw * 0.525;
        const double gap = 18;
        var wrap = new Grid();
        var wrapStack = new StackPanel();
        var sv = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.HorizontalOnly, Focusable = false };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < _hero.Count; i++)
        {
            int idx = i;
            var f = _hero[i];
            Store.Progress.TryGetValue(f.FullPath, out var pr);
            double? pct = pr != null && pr.Dur > 0 && pr.Pos > 5000 && pr.Pos < pr.Dur * 0.97 ? (double)pr.Pos / pr.Dur : null;
            var card = Poster(f, cw, ch, 28, true, pct, i, () => Play(_hero, idx));
            card.Margin = new Thickness(0, 0, gap, 0);
            row.Children.Add(card);
        }
        sv.Content = row;
        sv.PreviewMouseWheel += (s, e) =>
        {
            e.Handled = true;
            _scroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent, Source = s });
        };

        var dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
        var dotEls = new List<Border>();
        for (int i = 0; i < _hero.Count; i++)
        {
            int idx = i;
            var d = new Border { Height = 6, Width = i == 0 ? 24 : 6, CornerRadius = new CornerRadius(3), Margin = new Thickness(3, 0, 3, 0), Cursor = Cursors.Hand, Background = Util.Brush(i == 0 ? "#F08440" : "#66808090") };
            d.MouseLeftButtonUp += (_, _) => ScrollAnim.To(sv, idx * (cw + gap));
            dotEls.Add(d);
            dots.Children.Add(d);
        }

        int Nearest() => Math.Clamp((int)Math.Round(sv.HorizontalOffset / (cw + gap)), 0, Math.Max(0, _hero.Count - 1));
        void SetIdx(int idx)
        {
            if (idx == _heroIdx && dotEls.Count > 0 && dotEls[idx].Width > 20) return;
            _heroIdx = idx;
            for (int i = 0; i < dotEls.Count; i++)
            {
                Anim.To(dotEls[i], WidthProperty, i == idx ? 24 : 6, 350, Anim.Spring);
                dotEls[i].Background = Util.Brush(i == idx ? "#F08440" : "#66808090");
            }
            if (idx < _hero.Count && _thumbMem.TryGetValue(_hero[idx].FullPath, out var bmp)) SetBackdrop(bmp);
        }
        var snap = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        snap.Tick += (_, _) =>
        {
            snap.Stop();
            double target = Nearest() * (cw + gap);
            if (Math.Abs(sv.HorizontalOffset - target) > 1) ScrollAnim.To(sv, target);
        };
        sv.ScrollChanged += (_, e) => { if (e.HorizontalChange != 0) { SetIdx(Nearest()); snap.Stop(); snap.Start(); } };
        _heroIdx = 0;

        wrap.Children.Add(sv);
        if (_hero.Count > 1)
        {
            var left = Ui.IconButton("chevl", () => ScrollAnim.To(sv, Math.Max(0, (Nearest() - 1)) * (cw + gap)), 46);
            var rightB = Ui.IconButton("chev", () => ScrollAnim.To(sv, Math.Min(_hero.Count - 1, Nearest() + 1) * (cw + gap)), 46);
            left.HorizontalAlignment = HorizontalAlignment.Left; rightB.HorizontalAlignment = HorizontalAlignment.Right;
            left.Margin = new Thickness(10, 0, 0, 0); rightB.Margin = new Thickness(0, 0, 10, 0);
            left.Opacity = 0; rightB.Opacity = 0;
            wrap.MouseEnter += (_, _) => { Anim.Fade(left, 1, 180); Anim.Fade(rightB, 1, 180); };
            wrap.MouseLeave += (_, _) => { Anim.Fade(left, 0, 180); Anim.Fade(rightB, 0, 180); };
            wrap.Children.Add(left); wrap.Children.Add(rightB);
        }
        wrapStack.Children.Add(wrap);
        if (_hero.Count > 1) wrapStack.Children.Add(dots);
        return wrapStack;
    }

    // ---- grid of cards ----
    UIElement BuildGrid(List<MediaItem> list, double avail)
    {
        const double gap = 20;
        int cols = Math.Max(1, (int)((avail + gap) / (240 + gap)));
        double w = (avail - gap * (cols - 1)) / cols - 0.5;
        var wp = new WrapPanel { Margin = new Thickness(0, 0, -gap, 0) };
        for (int i = 0; i < list.Count; i++)
        {
            int idx = i;
            var card = Poster(list[i], w, w * 0.6, 20, false, null, i, () => Play(list, idx));
            card.Margin = new Thickness(0, 0, gap, gap);
            wp.Children.Add(card);
        }
        return wp;
    }

    /// <summary>Thumbnail card (hero = big carousel card, otherwise grid card).</summary>
    FrameworkElement Poster(MediaItem f, double w, double h, double radius, bool hero, double? progress, int index, Action click)
    {
        var root = new Grid { Width = w, Height = h, Cursor = Cursors.Hand, Opacity = 0 };
        var sc = new ScaleTransform(1, 1);
        var tr = new TranslateTransform(0, 26);
        var tg = new TransformGroup();
        tg.Children.Add(sc); tg.Children.Add(tr);
        root.RenderTransformOrigin = new Point(0.5, 0.5);
        root.RenderTransform = tg;
        var cr = new CornerRadius(radius);

        var baseB = new Border { CornerRadius = cr, BorderThickness = new Thickness(1), BorderBrush = Util.Brush("#1AFFFFFF") };
        baseB.SetResourceReference(Border.BackgroundProperty, "Surface");
        var imgB = new Border { CornerRadius = cr, Opacity = 0 };
        var shade = new Border { CornerRadius = cr, Background = new LinearGradientBrush(new GradientStopCollection { new GradientStop(Colors.Transparent, hero ? 0.35 : 0.4), new GradientStop(Color.FromArgb(225, 0, 0, 0), 1) }, 90) };
        root.Children.Add(baseB); root.Children.Add(imgB); root.Children.Add(shade);

        var dur = new TextBlock { Foreground = Brushes.White, FontSize = hero ? 12 : 11, FontWeight = FontWeights.Bold };
        lock (Store.Durations) dur.Text = Store.Durations.TryGetValue(f.FullPath, out var dd) ? Util.Fmt(dd) : "··";
        if (dur.Text == "··") FillDuration(f, dur);

        UIElement playIcon;
        if (hero)
        {
            var tag = new Border { CornerRadius = new CornerRadius(12), Background = Util.Brush("#66000000"), Padding = new Thickness(11, 5, 11, 5), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
            tag.Child = new TextBlock { Text = progress != null ? "Continue watching" : "Latest", Foreground = Brushes.White, FontSize = 11.5, FontWeight = FontWeights.Bold };
            root.Children.Add(tag);
            var info = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(22, 0, 96, 22) };
            info.Children.Add(new TextBlock { Text = f.Title, Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, MaxHeight = 52, TextTrimming = TextTrimming.CharacterEllipsis });
            var meta = new TextBlock { Foreground = Util.Brush("#C8FFFFFF"), FontSize = 12.5 };
            meta.Text = progress != null && Store.Progress.TryGetValue(f.FullPath, out var pr2) ? Util.Fmt(pr2.Dur - pr2.Pos) + " left" : Util.Human(f.Size);
            info.Children.Add(meta);
            root.Children.Add(info);
            var go = new Border { Width = 54, Height = 54, CornerRadius = new CornerRadius(27), Background = Ui.BrandGradient(), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 20, 20), Effect = new DropShadowEffect { Color = Util.C("#F08440"), BlurRadius = 20, ShadowDepth = 4, Opacity = 0.5 } };
            go.Child = Icons.Make("play", 30, fill: Brushes.White);
            root.Children.Add(go);
            playIcon = go;
        }
        else
        {
            var badge = new Border { CornerRadius = new CornerRadius(8), Background = Util.Brush("#73000000"), Padding = new Thickness(8, 2, 8, 2), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10), Child = dur };
            root.Children.Add(badge);
            var info = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(14, 0, 14, 12) };
            info.Children.Add(new TextBlock { Text = f.Title, Foreground = Brushes.White, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxHeight = 38, TextTrimming = TextTrimming.CharacterEllipsis });
            info.Children.Add(new TextBlock { Text = Util.Human(f.Size), Foreground = Util.Brush("#B3FFFFFF"), FontSize = 11 });
            root.Children.Add(info);
            var pc = new Border { Width = 48, Height = 48, CornerRadius = new CornerRadius(24), Background = Util.Brush("#F2F08440"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new ScaleTransform(0.6, 0.6) };
            pc.Child = Icons.Make("play", 26, fill: Brushes.White);
            root.Children.Add(pc);
            playIcon = pc;
        }
        if (progress != null)
        {
            var pbg = new Border { Height = 4, VerticalAlignment = VerticalAlignment.Bottom, Background = Util.Brush("#40FFFFFF"), CornerRadius = new CornerRadius(0, 0, radius, radius) };
            var pfill = new Border { Height = 4, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Width = w * progress.Value, Background = Util.Brush("#F08440") };
            root.Children.Add(pbg); root.Children.Add(pfill);
        }

        // thumbnail
        LoadThumb(f, bmp =>
        {
            imgB.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
            Anim.Fade(imgB, 1, 500);
            if (hero && _hero.Count > _heroIdx && ReferenceEquals(_hero[_heroIdx], f)) SetBackdrop(bmp);
        });

        // interaction
        root.MouseEnter += (_, _) =>
        {
            Anim.Scale(sc, hero ? 1.015 : 1.04);
            if (!hero && playIcon is Border pc && pc.RenderTransform is ScaleTransform pcs) { Anim.Fade(pc, 1, 180); Anim.Scale(pcs, 1, 350); }
        };
        root.MouseLeave += (_, _) =>
        {
            Anim.Scale(sc, 1);
            if (!hero && playIcon is Border pc && pc.RenderTransform is ScaleTransform pcs) { Anim.Fade(pc, 0, 150); Anim.Scale(pcs, 0.6, 200); }
        };
        root.PreviewMouseLeftButtonDown += (_, _) => Anim.Scale(sc, 0.97, 90, Anim.Smooth);
        root.PreviewMouseLeftButtonUp += (_, _) => Anim.Scale(sc, root.IsMouseOver ? (hero ? 1.015 : 1.04) : 1);
        root.MouseLeftButtonUp += (_, _) => click();

        // entrance
        int delay = Math.Min(index, 14) * 35;
        Anim.To(root, UIElement.OpacityProperty, 1, 450, Anim.Smooth, delay);
        Anim.To(tr, TranslateTransform.YProperty, 0, 600, Anim.Spring, delay);
        return root;
    }

    async void LoadThumb(MediaItem f, Action<BitmapSource> apply)
    {
        if (f.IsAudio) return;
        if (_thumbMem.TryGetValue(f.FullPath, out var b)) { apply(b); return; }
        await _thumbSem.WaitAsync();
        BitmapSource bmp = null;
        try { bmp = await Task.Run(() => Thumbs.Load(f)); } catch { }
        finally { _thumbSem.Release(); }
        if (bmp != null) { _thumbMem[f.FullPath] = bmp; apply(bmp); }
    }

    async void FillDuration(MediaItem f, TextBlock tb)
    {
        var d = await Durations.Get(f);
        if (d > 0) tb.Text = Util.Fmt(d);
    }

    UIElement FolderRow(string name, string path, int count, int fresh, int index, Action click)
    {
        var row = new Border { CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(1), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand, Opacity = 0 };
        row.SetResourceReference(Border.BackgroundProperty, "Glass");
        row.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var ic = new Border { Width = 66, Height = 54, CornerRadius = new CornerRadius(14), Background = Util.Brush("#33808090") };
        var icg = new Grid();
        icg.Children.Add(Icons.Make("folder", 34, "Muted"));
        if (fresh > 0)
        {
            var bd = new Border { MinWidth = 22, Height = 22, CornerRadius = new CornerRadius(11), Background = Util.Brush("#FF4D67"), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -8, -8, 0), Padding = new Thickness(6, 0, 6, 0) };
            bd.Child = new TextBlock { Text = fresh.ToString(), Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            icg.Children.Add(bd);
        }
        ic.Child = icg;
        var tx = new StackPanel { Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        tx.Children.Add(Ui.Text(name, 15, FontWeights.SemiBold));
        tx.Children.Add(Ui.Text($"{count} videos · {path}", 12.5, null, "Muted"));
        Grid.SetColumn(tx, 1);
        g.Children.Add(ic); g.Children.Add(tx);
        row.Child = g;
        Hover(row, click, index);
        return row;
    }

    UIElement AudioRow(MediaItem f, int index, Action click)
    {
        var row = new Border { CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(1), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand, Opacity = 0 };
        row.SetResourceReference(Border.BackgroundProperty, "Glass");
        row.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var ic = new Border { Width = 50, Height = 50, CornerRadius = new CornerRadius(14), Background = Ui.BrandGradient(), Child = Icons.Make("music", 24, fill: Brushes.White) };
        var tx = new StackPanel { Margin = new Thickness(14, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
        tx.Children.Add(Ui.Text(f.Title, 15, FontWeights.SemiBold));
        var meta = Ui.Text($"{f.FolderName} · {Util.Human(f.Size)}", 12.5, null, "Muted");
        tx.Children.Add(meta);
        Grid.SetColumn(tx, 1);
        var dur = Ui.Text("", 12.5, null, "Muted");
        dur.VerticalAlignment = VerticalAlignment.Center;
        lock (Store.Durations) dur.Text = Store.Durations.TryGetValue(f.FullPath, out var dd) ? Util.Fmt(dd) : "";
        if (dur.Text.Length == 0) FillDuration(f, dur);
        Grid.SetColumn(dur, 2);
        g.Children.Add(ic); g.Children.Add(tx); g.Children.Add(dur);
        row.Child = g;
        Hover(row, click, index);
        return row;
    }

    void Hover(Border row, Action click, int index)
    {
        var tr = new TranslateTransform(0, 18);
        var sc = new ScaleTransform(1, 1);
        var tg = new TransformGroup();
        tg.Children.Add(sc); tg.Children.Add(tr);
        row.RenderTransformOrigin = new Point(0.5, 0.5);
        row.RenderTransform = tg;
        row.MouseEnter += (_, _) => Anim.Scale(sc, 1.008);
        row.MouseLeave += (_, _) => Anim.Scale(sc, 1);
        row.PreviewMouseLeftButtonDown += (_, _) => Anim.Scale(sc, 0.98, 90, Anim.Smooth);
        row.PreviewMouseLeftButtonUp += (_, _) => Anim.Scale(sc, row.IsMouseOver ? 1.008 : 1);
        row.MouseLeftButtonUp += (_, _) => click();
        int delay = Math.Min(index, 14) * 30;
        Anim.To(row, UIElement.OpacityProperty, 1, 400, Anim.Smooth, delay);
        Anim.To(tr, TranslateTransform.YProperty, 0, 500, Anim.Spring, delay);
    }

    // =====================================================================
    //  Actions
    // =====================================================================
    MediaItem ComputeLast()
    {
        MediaItem best = null; long t = 0;
        var byPath = _files.ToDictionary(f => f.FullPath, f => f, StringComparer.OrdinalIgnoreCase);
        foreach (var p in Store.Progress) if (byPath.TryGetValue(p.Key, out var f) && p.Value.T > t) { t = p.Value.T; best = f; }
        return best;
    }

    void PlayLast()
    {
        if (_last == null) return;
        var list = Sorted(_files.Where(f => f.IsAudio == _last.IsAudio));
        Play(list, Math.Max(0, list.FindIndex(f => f.FullPath == _last.FullPath)));
    }

    public void Play(List<MediaItem> list, int index)
    {
        if (list == null || list.Count == 0) return;
        if (PlayerWindow.Current != null) { PlayerWindow.Current.ReplaceQueue(list, index); return; }
        var w = new PlayerWindow(list, index);
        w.Closed += (_, _) =>
        {
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Show(); Activate();
            Render();
        };
        Hide();
        w.Show();
    }

    /// <summary>Handles command-line / Explorer right-click requests. Returns true if a player was started.</summary>
    public bool HandleArgs(string[] args)
    {
        var files = new List<string>();
        string add = null, playFolder = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--add-folder" && i + 1 < args.Length) add = args[++i];
            else if (args[i] == "--play-folder" && i + 1 < args.Length) playFolder = args[++i];
            else files.Add(args[i]);
        }
        if (add != null && Directory.Exists(add)) AddFolder(add);
        var items = new List<MediaItem>();
        if (playFolder != null && Directory.Exists(playFolder))
            items.AddRange(Library.Scan(new[] { playFolder }).OrderBy(f => f.FullPath, Comparer<string>.Create(Util.NatCmp)));
        foreach (var f in files)
        {
            if (Directory.Exists(f)) { AddFolder(f); continue; }
            if (File.Exists(f) && Library.IsMedia(f)) items.Add(Library.For(f));
        }
        if (items.Count == 0) return false;
        Play(items, 0);
        return true;
    }

    async void AddFolder(string path)
    {
        if (!Store.Cfg.Folders.Contains(path, StringComparer.OrdinalIgnoreCase)) { Store.Cfg.Folders.Add(path); Store.SaveCfg(); }
        _files = await Task.Run(() => Library.Scan(Store.Cfg.Folders));
        Render();
        Toasts.Show(_root, "Folder added to library");
    }

    void AddFolderDialog()
    {
        var d = new Microsoft.Win32.OpenFolderDialog { Title = "Add a folder to your Cascade library" };
        if (d.ShowDialog(this) == true) AddFolder(d.FolderName);
    }

    void OpenFiles()
    {
        var d = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true, Title = "Open media",
            Filter = "Media files|" + string.Join(";", Library.Video.Concat(Library.Audio).Select(x => "*" + x)) + "|All files|*.*",
        };
        if (d.ShowDialog(this) == true)
        {
            var items = d.FileNames.Where(Library.IsMedia).Select(Library.For).ToList();
            if (items.Count > 0) Play(items, 0);
        }
    }

    void OpenUrl()
    {
        var box = new TextBox { Height = 42, FontSize = 14, Padding = new Thickness(12, 0, 12, 0), VerticalContentAlignment = VerticalAlignment.Center, BorderThickness = new Thickness(1) };
        box.SetResourceReference(TextBox.BackgroundProperty, "Glass");
        box.SetResourceReference(TextBox.ForegroundProperty, "Fg");
        box.SetResourceReference(TextBox.BorderBrushProperty, "GlassBorder");
        var sp = new StackPanel();
        sp.Children.Add(Ui.Text("Play network URL", 20, FontWeights.Bold));
        sp.Children.Add(new Border { Height = 14 });
        sp.Children.Add(box);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var cancel = new Button { Style = (Style)FindResource("PillBtn"), Content = "Cancel", Margin = new Thickness(0, 0, 10, 0) };
        var ok = new Button { Style = (Style)FindResource("PrimaryBtn"), Content = "Play" };
        actions.Children.Add(cancel); actions.Children.Add(ok);
        sp.Children.Add(actions);
        var close = Modal.Show(_root, sp);
        box.Focus();
        void Go()
        {
            var u = box.Text.Trim();
            if (u.Length == 0) return;
            close();
            var audio = new[] { ".mp3", ".m4a", ".aac", ".flac", ".wav", ".ogg", ".opus" }.Any(x => u.Split('?')[0].EndsWith(x, StringComparison.OrdinalIgnoreCase));
            Play(new List<MediaItem> { new MediaItem { FullPath = u, IsUrl = true, Title = u.Split('?')[0].Split('/').Last(), FolderName = "Network", IsAudio = audio } }, 0);
        }
        cancel.Click += (_, _) => close();
        ok.Click += (_, _) => Go();
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) Go(); };
    }

    void ManageFolders()
    {
        var sp = new StackPanel();
        Action close = null;
        void Draw()
        {
            sp.Children.Clear();
            sp.Children.Add(Ui.Text("Library folders", 20, FontWeights.Bold));
            sp.Children.Add(new Border { Height = 12 });
            if (Store.Cfg.Folders.Count == 0) sp.Children.Add(Ui.Text("No folders yet.", 14, null, "Muted"));
            foreach (var f in Store.Cfg.Folders.ToList())
            {
                var row = new Border { CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 8, 8, 8), Margin = new Thickness(0, 0, 0, 8) };
                row.SetResourceReference(Border.BackgroundProperty, "Glass");
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var t = Ui.Text(f, 13); t.VerticalAlignment = VerticalAlignment.Center;
                var rm = new Button { Style = (Style)FindResource("PillBtn"), Content = "Remove" };
                var path = f;
                rm.Click += async (_, _) =>
                {
                    Store.Cfg.Folders.Remove(path); Store.SaveCfg(); Draw();
                    _files = await Task.Run(() => Library.Scan(Store.Cfg.Folders)); Render();
                };
                Grid.SetColumn(rm, 1);
                g.Children.Add(t); g.Children.Add(rm);
                row.Child = g;
                sp.Children.Add(row);
            }
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var add = new Button { Style = (Style)FindResource("PillBtn"), Content = "Add folder…", Margin = new Thickness(0, 0, 10, 0) };
            var done = new Button { Style = (Style)FindResource("PrimaryBtn"), Content = "Done" };
            add.Click += async (_, _) =>
            {
                var d = new Microsoft.Win32.OpenFolderDialog();
                if (d.ShowDialog(this) == true && !Store.Cfg.Folders.Contains(d.FolderName, StringComparer.OrdinalIgnoreCase))
                {
                    Store.Cfg.Folders.Add(d.FolderName); Store.SaveCfg(); Draw();
                    _files = await Task.Run(() => Library.Scan(Store.Cfg.Folders)); Render();
                }
            };
            done.Click += (_, _) => close();
            actions.Children.Add(add); actions.Children.Add(done);
            sp.Children.Add(actions);
        }
        Draw();
        close = Modal.Show(_root, sp);
    }
}
