using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DTM.Data.Terminal;
using NLog;
using SystemFile = System.IO.File;

namespace DTM.Views.Controls;

public enum TerminalKind
{
    Ssh,
    PowerShell
}

public partial class ConsoleControl : UserControl
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    // ----- Avalonia Properties --------------------------------------------------

    public static readonly StyledProperty<TerminalKind> KindProperty =
        AvaloniaProperty.Register<ConsoleControl, TerminalKind>(nameof(Kind), defaultValue: TerminalKind.Ssh);

    public static readonly StyledProperty<string> HostProperty =
        AvaloniaProperty.Register<ConsoleControl, string>(nameof(Host), defaultValue: string.Empty);

    public static readonly StyledProperty<string> UserProperty =
        AvaloniaProperty.Register<ConsoleControl, string>(nameof(User), defaultValue: string.Empty);

    public static readonly StyledProperty<int> PortProperty =
        AvaloniaProperty.Register<ConsoleControl, int>(nameof(Port), defaultValue: 22);

    public static readonly StyledProperty<string> InitialScriptProperty =
        AvaloniaProperty.Register<ConsoleControl, string>(nameof(InitialScript), defaultValue: string.Empty);

    public TerminalKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string Host
    {
        get => GetValue(HostProperty);
        set => SetValue(HostProperty, value);
    }

    public string User
    {
        get => GetValue(UserProperty);
        set => SetValue(UserProperty, value);
    }

    public int Port
    {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    public string InitialScript
    {
        get => GetValue(InitialScriptProperty);
        set => SetValue(InitialScriptProperty, value);
    }

    // ----- Internals ------------------------------------------------------------

    private ITerminalSession? _session;
    private string? _startedSignature;       // dient als "Verbindungsfingerprint"
    private bool _isAttached;
    private bool _windowCloseRegistered;

    public ConsoleControl()
    {
        InitializeComponent();
    }

    private string CurrentSignature() => Kind switch
    {
        TerminalKind.Ssh        => $"ssh|{User}@{Host}:{Port}",
        TerminalKind.PowerShell => $"pwsh|{InitialScript.GetHashCode()}",
        _ => "unknown"
    };

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = true;
        base.OnAttachedToVisualTree(e);

        if (!_windowCloseRegistered && e.Root is Window w)
        {
            _windowCloseRegistered = true;
            w.Closed += (_, _) => Stop();
        }

        if (_session is null || !_session.IsRunning || _startedSignature != CurrentSignature())
        {
            Stop();
            Start();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        // Session bleibt absichtlich am Leben, damit Tab-Wechsel die Verbindung nicht killt.
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        bool relevant = change.Property == HostProperty
                     || change.Property == UserProperty
                     || change.Property == PortProperty
                     || change.Property == KindProperty
                     || change.Property == InitialScriptProperty;

        if (relevant && _isAttached && _startedSignature != CurrentSignature())
        {
            Stop();
            Start();
        }
    }

    // ----- Lifecycle ------------------------------------------------------------

    public void Start()
    {
        if (_session is { IsRunning: true }) return;

        try
        {
            _session = Kind switch
            {
                TerminalKind.Ssh        => CreateSshSession(),
                TerminalKind.PowerShell => CreatePowerShellSession(),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Konnte Terminal-Session nicht erstellen.");
            Append($"[Fehler beim Erstellen der Session: {ex.Message}]", "indianred");
            return;
        }

        if (_session is null)
        {
            Append("[Kein Terminal-Backend für aktuelle Konfiguration]", "indianred");
            return;
        }

        WireUp(_session);
        // pwsh-Session am Bus registrieren, damit Hintergrund-Jobs (z.B. Backup)
        // live im Tab erscheinen statt in einem unsichtbaren Subprozess.
        if (Kind == TerminalKind.PowerShell)
            TerminalBus.RegisterPowerShellSession(_session);

        _startedSignature = CurrentSignature();
        _ = _session.StartAsync();
    }

    private ITerminalSession? CreateSshSession()
    {
        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(User))
        {
            Append("[SSH: Bitte zuerst eine Oracle-Datenbank auswählen]", "gray", appendNewline: true);
            return null;
        }
        return new SshTerminalSession(Host, User, Port);
    }

    private ITerminalSession CreatePowerShellSession()
    {
        string? init = string.IsNullOrWhiteSpace(InitialScript) ? null : InitialScript;
        return new PowerShellTerminalSession(init, autoRouteThroughSession: true);
    }

    private void WireUp(ITerminalSession session)
    {
        session.OutputReceived += (_, text) => Append(text, "OUT", appendNewline: false);
        session.ErrorReceived  += (_, text) => Append(text, "ERR", appendNewline: false);
        session.Notice         += (_, text) => Append(text, "NTC", appendNewline: true);
        session.SessionEnded   += (_, _)    => Append("Session beendet", "NTC", appendNewline: true);
    }

    public void Stop()
    {
        if (_session is null) return;
        // Bus-Registrierung zuerst lösen, damit kein Backup-Job nach Dispose
        // versucht die tote Session zu nutzen.
        if (Kind == TerminalKind.PowerShell)
            TerminalBus.UnregisterPowerShellSession(_session);
        try { _session.Dispose(); }
        catch { /* swallow */ }
        _session = null;
        _startedSignature = null;
    }

    public void SendCommand(string cmd)
    {
        if (_session is null || !_session.IsRunning)
        {
            Append("Keine aktive Session", "ERR", appendNewline: true);
            return;
        }

        // Echo den Befehl lokal — aber nur für PowerShell. Bei SSH echo't das
        // Remote-PTY den Befehl selbst zurück (siehe v4-Logs); ein lokales
        // Echo würde jeden Befehl doppelt anzeigen.
        if (Kind == TerminalKind.PowerShell)
            Append($"> {cmd}", "ECHO", appendNewline: true);

        _ = _session.SendCommandAsync(cmd);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        string cmd = InputBox.Text ?? string.Empty;
        InputBox.Text = string.Empty;
        SendCommand(cmd);
    }

    /// <summary>
    /// Routet Stream-Output an die <see cref="AnsiConsole"/> mit angemessenem
    /// Styling. SSH-Output enthält ANSI-Codes, die der Parser farbig
    /// interpretiert. Meta-Streams (Notice/Error/Echo) bekommen feste Farben.
    /// Zusätzlich Diagnose-Log nach %TEMP%/dtm-console.log.
    /// </summary>
    private void Append(string? text, string kind, bool appendNewline = false)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Diagnose-Log (best effort, niemals werfen).
        try
        {
            string logPath = Path.Combine(Path.GetTempPath(), "dtm-console.log");
            SystemFile.AppendAllText(logPath,
                $"{DateTime.Now:HH:mm:ss.fff} [{kind}] {text.Replace("\r","\\r").Replace("\n","\\n")}{Environment.NewLine}");
        }
        catch { /* swallow */ }

        string display = appendNewline ? text + "\n" : text;
        switch (kind)
        {
            case "OUT":
                // ANSI-Codes durch den Parser, Farben übernehmen.
                Output.Append(display);
                break;
            case "ERR":
                Output.AppendLine(display, new AnsiStyle(
                    Foreground: new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0xCD, 0x5C, 0x5C)),
                    Background: null, Bold: false, Italic: false, Underline: false));
                break;
            case "NTC":
                Output.AppendLine(display, new AnsiStyle(
                    Foreground: new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x87, 0xCE, 0xFA)),
                    Background: null, Bold: false, Italic: false, Underline: false));
                break;
            case "ECHO":
                Output.AppendLine(display, new AnsiStyle(
                    Foreground: new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x90, 0xEE, 0x90)),
                    Background: null, Bold: true, Italic: false, Underline: false));
                break;
            default:
                Output.Append(display);
                break;
        }
    }
}
