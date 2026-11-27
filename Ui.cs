using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using WShapes = System.Windows.Shapes;

namespace Cascade;

public static class Anim
{
    public static readonly IEasingFunction Spring = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.7 };
    public static readonly IEasingFunction Smooth = new CubicEase { EasingMode = EasingMode.EaseOut };

    public static void To(IAnimatable target, DependencyProperty prop, double to, int ms = 250, IEasingFunction ease = null, int delay = 0)
    {
        var a = new DoubleAnimation { To = to, Duration = TimeSpan.FromMilliseconds(ms), EasingFunction = ease ?? Smooth, BeginTime = TimeSpan.FromMilliseconds(delay) };
        target.BeginAnimation(prop, a);
    }

    public static void Scale(ScaleTransform s, double v, int ms = 250, IEasingFunction ease = null)
    {
        To(s, ScaleTransform.ScaleXProperty, v, ms, ease ?? Spring);
        To(s, ScaleTransform.ScaleYProperty, v, ms, ease ?? Spring);
    }

    public static void Fade(UIElement e, double to, int ms = 250, int delay = 0) => To(e, UIElement.OpacityProperty, to, ms, Smooth, delay);

    /// <summary>Hover grow + press shrink on any element. Returns its scale transform.</summary>
    public static ScaleTransform Hover(FrameworkElement el, double hover = 1.04, double press = 0.96)
    {
        var s = new ScaleTransform(1, 1);
        el.RenderTransformOrigin = new Point(0.5, 0.5);
        el.RenderTransform = s;
        el.MouseEnter += (_, _) => Scale(s, hover);
        el.MouseLeave += (_, _) => Scale(s, 1);
        el.PreviewMouseLeftButtonDown += (_, _) => Scale(s, press, 90, Smooth);
        el.PreviewMouseLeftButtonUp += (_, _) => Scale(s, el.IsMouseOver ? hover : 1);
        return s;
    }
}

public static class Ui
{
    public static LinearGradientBrush BrandGradient() => new LinearGradientBrush(Util.C("#F69A5C"), Util.C("#EE7E3E"), 45);

    public static TextBlock Text(string s, double size = 14, FontWeight? weight = null, string resKey = "Fg")
    {
        var t = new TextBlock { Text = s, FontSize = size, FontWeight = weight ?? FontWeights.Normal, TextTrimming = TextTrimming.CharacterEllipsis };
        t.SetResourceReference(TextBlock.ForegroundProperty, resKey);
        return t;
    }

    public static Button IconButton(string icon, Action click, double size = 40, string tip = null, string resKey = "Fg")
    {
        var b = new Button { Style = (Style)Application.Current.FindResource("GlassBtn"), Width = size, Height = size, ToolTip = tip, Content = Icons.Make(icon, size * 0.5, resKey) };
        b.Click += (_, _) => click();
        return b;
    }

    public static Border Logo(double size, bool animated = false)
    {
        var b = new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size * 0.26), Background = BrandGradient() };
        var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        double[] hs = { 0.16, 0.30, 0.42, 0.26, 0.36, 0.18 };
        double bw = size * 0.058, gap = size * 0.085;
        for (int i = 0; i < hs.Length; i++)
        {
            var bar = new Border
            {
                Width = bw, Height = size * hs[i], CornerRadius = new CornerRadius(bw / 2), Background = Brushes.White,
                Margin = new Thickness((gap - bw) / 2, 0, (gap - bw) / 2, 0), VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
            };
            if (animated)
            {
                var st = new ScaleTransform(1, 1);
                bar.RenderTransform = st;
                var a = new DoubleAnimation(0.55, 1.25, TimeSpan.FromMilliseconds(520 + i * 110)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            sp.Children.Add(bar);
        }
        b.Child = sp;
        return b;
    }
}

public static class Menus
{
    public static void Show(FrameworkElement target, IEnumerable<(string label, Action act, bool selected)> items)
    {
        var panel = new StackPanel { Margin = new Thickness(6) };
        var scale = new ScaleTransform(0.94, 0.94);
        var border = new Border
        {
            CornerRadius = new CornerRadius(16), MinWidth = 230, BorderThickness = new Thickness(1), Child = panel, Opacity = 0,
            RenderTransformOrigin = new Point(1, 0), RenderTransform = scale, Margin = new Thickness(14),
            Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.35, Color = Colors.Black },
        };
        border.SetResourceReference(Border.BackgroundProperty, "MenuBg");
        border.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        var pop = new Popup
        {
            PlacementTarget = target, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true,
            PopupAnimation = PopupAnimation.None, Child = border, HorizontalOffset = target.ActualWidth - 258, VerticalOffset = -6,
        };
        foreach (var (label, act, sel) in items)
        {
            var row = new Grid();
            row.Children.Add(Text(label, 14));
            if (sel) { var ck = Text("✓", 14, FontWeights.Bold, "Brand"); ck.HorizontalAlignment = HorizontalAlignment.Right; row.Children.Add(ck); }
            var btn = new Button { Style = (Style)Application.Current.FindResource("MenuBtn"), Content = row };
            var a = act;
            btn.Click += (_, _) => { pop.IsOpen = false; a(); };
            panel.Children.Add(btn);
        }
        pop.IsOpen = true;
        Anim.Fade(border, 1, 160);
        Anim.Scale(scale, 1, 300);
    }

    static TextBlock Text(string s, double size, FontWeight? w = null, string key = "Fg") => Ui.Text(s, size, w, key);
}

public static class Modal
{
    public static Action Show(Grid host, FrameworkElement content)
    {
        var dim = new Grid { Background = Util.Brush("#99000000"), Opacity = 0 };
        var scale = new ScaleTransform(0.9, 0.9);
        var card = new Border
        {
            CornerRadius = new CornerRadius(26), Padding = new Thickness(24), MinWidth = 400, MaxWidth = 540, MaxHeight = 640,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(1), Child = content, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = scale,
        };
        card.SetResourceReference(Border.BackgroundProperty, "Surface");
        card.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        dim.Children.Add(card);
        Panel.SetZIndex(dim, 100);
        Grid.SetRowSpan(dim, 99);
        Grid.SetColumnSpan(dim, 99);
        host.Children.Add(dim);
        bool closing = false;
        Action close = () =>
        {
            if (closing) return;
            closing = true;
            var a = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
            a.Completed += (_, _) => host.Children.Remove(dim);
            dim.BeginAnimation(UIElement.OpacityProperty, a);
        };
        dim.MouseLeftButtonDown += (_, e) => { if (ReferenceEquals(e.OriginalSource, dim)) close(); };
        Anim.Fade(dim, 1, 180);
        Anim.Scale(scale, 1, 380);
        return close;
    }
}

public static class Toasts
{
    public static void Show(Panel host, string msg, string action = null, Action onAction = null)
    {
        var tr = new TranslateTransform(0, 24);
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new TextBlock { Text = msg, Foreground = Brushes.White, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
        var b = new Border
        {
            CornerRadius = new CornerRadius(22), Background = Util.Brush("#F2191A1F"), BorderBrush = Util.Brush("#1FFFFFFF"), BorderThickness = new Thickness(1),
            Padding = new Thickness(20, 12, 20, 12), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 110), Opacity = 0, RenderTransform = tr, Child = row, IsHitTestVisible = action != null,
        };
        Panel.SetZIndex(b, 200);
        Grid.SetRowSpan(b, 99);
        Grid.SetColumnSpan(b, 99);
        void Remove()
        {
            var a = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250));
            a.Completed += (_, _) => host.Children.Remove(b);
            b.BeginAnimation(UIElement.OpacityProperty, a);
        }
        if (action != null)
        {
            var t = new TextBlock { Text = action, Foreground = Util.Brush("#F08440"), FontWeight = FontWeights.Bold, Margin = new Thickness(18, 0, 0, 0), Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
            t.MouseLeftButtonUp += (_, _) => { onAction?.Invoke(); Remove(); };
            row.Children.Add(t);
        }
        // only one toast at a time
        foreach (var old in host.Children.OfType<Border>().Where(x => x.Tag as string == "toast").ToList()) host.Children.Remove(old);
        b.Tag = "toast";
        host.Children.Add(b);
        Anim.Fade(b, 1, 200);
        Anim.To(tr, TranslateTransform.YProperty, 0, 420, Anim.Spring);
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(action != null ? 4500 : 2200) };
        timer.Tick += (_, _) => { timer.Stop(); if (host.Children.Contains(b)) Remove(); };
        timer.Start();
    }
}

/// <summary>Thin orange slider used for seeking and volume.</summary>
public class Bar : Grid
{
    readonly Border _track, _fill, _chip;
    readonly TextBlock _chipText;
    readonly WShapes.Ellipse _thumb;
    readonly ScaleTransform _thumbScale = new(1, 1);
    double _v;
    bool _drag;

    public Func<double, string> Label;           // chip text for a fraction 0..1
    public event Action<double> Dragging;
    public event Action<double> Committed;
    public bool IsDragging => _drag;

    public Bar(double height = 30)
    {
        Height = height;
        Background = Brushes.Transparent;
        Cursor = Cursors.Hand;
        var orange = Util.Brush("#F08440");
        _track = new Border { Height = 4, CornerRadius = new CornerRadius(2), Background = Util.Brush("#47FFFFFF"), VerticalAlignment = VerticalAlignment.Center };
        _fill = new Border { Height = 4, CornerRadius = new CornerRadius(2), Background = orange, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Width = 0 };
        _thumb = new WShapes.Ellipse { Width = 14, Height = 14, Fill = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = _thumbScale };
        _chipText = new TextBlock { Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold };
        _chip = new Border { Background = orange, CornerRadius = new CornerRadius(6), Padding = new Thickness(7, 3, 7, 3), Child = _chipText, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -22, 0, 0), Opacity = 0, IsHitTestVisible = false };
        Children.Add(_track); Children.Add(_fill); Children.Add(_thumb); Children.Add(_chip);
        SizeChanged += (_, _) => Lay();
        MouseEnter += (_, _) => Grow(true);
        MouseLeave += (_, _) => { if (!_drag) Grow(false); };
        MouseMove += (_, e) => { var f = Frac(e); ShowChip(f); if (_drag) Move(f); };
        MouseLeftButtonDown += (_, e) => { CaptureMouse(); _drag = true; Grow(true); Move(Frac(e)); };
        MouseLeftButtonUp += (_, e) =>
        {
            if (!_drag) return;
            _drag = false; ReleaseMouseCapture();
            var f = Frac(e); _v = f; Lay();
            Committed?.Invoke(f);
            if (!IsMouseOver) Grow(false);
        };
    }

    public void Set(double v) { if (_drag) return; _v = Math.Clamp(v, 0, 1); Lay(); }

    double Frac(MouseEventArgs e) => Math.Clamp(e.GetPosition(this).X / Math.Max(1, ActualWidth), 0, 1);

    void Move(double f) { _v = f; Lay(); Dragging?.Invoke(f); }

    void Lay()
    {
        double w = ActualWidth;
        _fill.Width = Math.Max(0, w * _v);
        _thumb.Margin = new Thickness(w * _v - 7, 0, 0, 0);
    }

    void ShowChip(double f)
    {
        if (Label == null) return;
        _chipText.Text = Label(f);
        _chip.Margin = new Thickness(Math.Clamp(f * ActualWidth - 24, 0, Math.Max(0, ActualWidth - 48)), -22, 0, 0);
        _chip.Opacity = 1;
    }

    void Grow(bool on)
    {
        Anim.To(_track, HeightProperty, on ? 8 : 4, 180);
        Anim.To(_fill, HeightProperty, on ? 8 : 4, 180);
        Anim.Scale(_thumbScale, on ? 1.35 : 1, 300);
        if (!on) _chip.Opacity = 0;
    }
}
