using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DTM.Data.Terminal;
using NLog;

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
        session.OutputReceived += (_, text) => Append(text, "#E0E0E0", appendNewline: false);
        session.ErrorReceived  += (_, text) => Append(text, "indianred", appendNewline: false);
        session.Notice         += (_, text) => Append(text, "lightskyblue", appendNewline: true);
        session.SessionEnded   += (_, _)    => Append("[Session beendet]", "gray", appendNewline: true);
    }

    public void Stop()
    {
        if (_session is null) return;
        try { _session.Dispose(); }
        catch { /* swallow */ }
        _session = null;
        _startedSignature = null;
    }

    public void SendCommand(string cmd)
    {
        if (_session is null || !_session.IsRunning)
        {
            Append("[Keine aktive Session]", "indianred", appendNewline: true);
            return;
        }

        // Für PowerShell echoen wir den Befehl lokal, weil das in-process-Runspace
        // keinen Prompt schreibt. Für SSH NICHT — das Remote-PTY echo't selbst.
        if (Kind == TerminalKind.PowerShell)
            Append($"> {cmd}", "lightgreen", appendNewline: true);

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

    private void Append(string? text, string color, bool appendNewline = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        Dispatcher.UIThread.Post(() =>
        {
            string display = appendNewline ? text + Environment.NewLine : text;
            var run = new Avalonia.Controls.Documents.Run(display)
            {
                Foreground = Avalonia.Media.Brush.Parse(color)
            };
            OutputBlock.Inlines?.Add(run);
            OutputScroll.ScrollToEnd();
        });
    }
}
