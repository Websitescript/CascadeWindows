using System.IO.Pipes;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;

namespace Cascade;

public partial class App : Application
{
    public static LibVLC Vlc;
    const string PipeName = "Cascade.Pipe.v2";
    Mutex _mutex;
    MainWindow _main;
    readonly List<string> _pending = new();
    DispatcherTimer _batch;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, "Cascade.Windows.SingleInstance.v2", out bool first);
        if (!first)
        {
            // Another Cascade is running: hand it our arguments (right-click "Play with Cascade") and exit.
            try
            {
                using var c = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                c.Connect(2000);
                using var w = new StreamWriter(c);
                w.Write(string.Join("\n", e.Args));
                w.Flush();
            }
            catch { }
            Shutdown();
            return;
        }

        Core.Initialize();
        Vlc = new LibVLC("--no-video-title-show", "--no-osd");
        Store.Init();
        Theme.Apply();

        _main = new MainWindow();
        MainWindow = _main;
        _main.Initialize();
        bool played = e.Args.Length > 0 && _main.HandleArgs(e.Args);
        if (!played) _main.Show();

        _batch = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _batch.Tick += (_, _) =>
        {
            _batch.Stop();
            var a = _pending.ToArray();
            _pending.Clear();
            if (a.Length > 0 && !_main.HandleArgs(a)) { if (!_main.IsVisible) _main.Show(); }
        };
        ListenForOtherInstances();
    }

    async void ListenForOtherInstances()
    {
        while (true)
        {
            try
            {
                using var s = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await s.WaitForConnectionAsync();
                using var r = new StreamReader(s);
                var text = await r.ReadToEndAsync();
                foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) _pending.Add(line);
                _batch.Stop();
                _batch.Start();   // several files from Explorer arrive within a few ms: handle them together
            }
            catch { await Task.Delay(300); }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { Vlc?.Dispose(); } catch { }
        base.OnExit(e);
    }
}
