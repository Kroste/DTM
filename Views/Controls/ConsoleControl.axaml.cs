using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using NLog;

namespace DTM.Views.Controls;

public partial class ConsoleControl : UserControl
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public static readonly StyledProperty<string> FileNameProperty =
        AvaloniaProperty.Register<ConsoleControl, string>(nameof(FileName), defaultValue: string.Empty);

    public static readonly StyledProperty<string> ArgumentsProperty =
        AvaloniaProperty.Register<ConsoleControl, string>(nameof(Arguments), defaultValue: string.Empty);

    public static readonly StyledProperty<string?> WorkingDirectoryProperty =
        AvaloniaProperty.Register<ConsoleControl, string?>(nameof(WorkingDirectory));

    public string FileName
    {
        get => GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    public string Arguments
    {
        get => GetValue(ArgumentsProperty);
        set => SetValue(ArgumentsProperty, value);
    }

    public string? WorkingDirectory
    {
        get => GetValue(WorkingDirectoryProperty);
        set => SetValue(WorkingDirectoryProperty, value);
    }

    private Process? _process;

    public ConsoleControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Stop();
        base.OnDetachedFromVisualTree(e);
    }

    public void Start()
    {
        if (_process is { HasExited: false }) return;
        if (string.IsNullOrWhiteSpace(FileName))
        {
            Append("[Kein FileName gesetzt]", "tomato");
            return;
        }

        if (!TryStartProcess(FileName) && OperatingSystem.IsWindows() && FileName.Equals("pwsh", StringComparison.OrdinalIgnoreCase))
        {
            Append("[pwsh nicht gefunden — versuche powershell.exe]", "lightskyblue");
            TryStartProcess("powershell.exe");
        }
    }

    private bool TryStartProcess(string fileName)
    {
        try
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = Arguments,
                    WorkingDirectory = WorkingDirectory ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            _process.OutputDataReceived += (_, e) => Append(e.Data, "#E0E0E0");
            _process.ErrorDataReceived += (_, e) => Append(e.Data, "indianred");
            _process.Exited += (_, _) => Append($"[Prozess beendet: {fileName}]", "gray");

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            Append($"[Gestartet: {fileName} {Arguments}]", "lightskyblue");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, $"Konnte '{fileName}' nicht starten.");
            Append($"[Fehler: {ex.Message}]", "indianred");
            _process?.Dispose();
            _process = null;
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                try { _process.StandardInput.Close(); } catch { /* ignore */ }
                if (!_process.WaitForExit(1500))
                {
                    try { _process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                }
            }
        }
        catch { /* swallow on shutdown */ }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        var cmd = InputBox.Text ?? string.Empty;
        InputBox.Text = string.Empty;

        if (_process is null || _process.HasExited)
        {
            Append("[Kein Prozess aktiv]", "indianred");
            return;
        }

        Append($"> {cmd}", "lightgreen");
        try { _process.StandardInput.WriteLine(cmd); }
        catch (Exception ex) { Append($"[Fehler: {ex.Message}]", "indianred"); }
    }

    private void Append(string? text, string color)
    {
        if (text is null) return;
        Dispatcher.UIThread.Post(() =>
        {
            var run = new Avalonia.Controls.Documents.Run(text + Environment.NewLine)
            {
                Foreground = Avalonia.Media.Brush.Parse(color)
            };
            OutputBlock.Inlines?.Add(run);
            OutputScroll.ScrollToEnd();
        });
    }
}
