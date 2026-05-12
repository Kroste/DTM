using System.Diagnostics;
using System.Text;
using System.ComponentModel;

namespace DTM;

public class ConsoleControl : UserControl
{
    private readonly RichTextBox _output;
    private readonly TextBox _input;
    private Process? _process;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FileName { get; set; } = "";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Arguments { get; set; } = "";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? WorkingDirectory { get; set; }

    public ConsoleControl()
    {
        _output = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.Gainsboro,
            Font = new Font("Consolas", 10f),
            DetectUrls = false,
            BorderStyle = BorderStyle.None,
            HideSelection = false
        };

        var inputPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Color.Black,
            Padding = new Padding(4, 2, 4, 2)
        };

        var prompt = new Label
        {
            Text = ">",
            Dock = DockStyle.Left,
            AutoSize = true,
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _input = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.Black,
            ForeColor = Color.White,
            Font = new Font("Consolas", 10f)
        };
        _input.KeyDown += Input_KeyDown;

        inputPanel.Controls.Add(_input);
        inputPanel.Controls.Add(prompt);

        Controls.Add(_output);
        Controls.Add(inputPanel);
    }

    public void Start()
    {
        if (_process is { HasExited: false }) return;
        if (string.IsNullOrWhiteSpace(FileName)) return;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FileName,
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

        _process.OutputDataReceived += (_, e) => Append(e.Data, Color.Gainsboro);
        _process.ErrorDataReceived += (_, e) => Append(e.Data, Color.IndianRed);
        _process.Exited += (_, _) => Append($"[Prozess beendet: {FileName}]", Color.Gray);

        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            Append($"[Gestartet: {FileName} {Arguments}]", Color.LightSkyBlue);
        }
        catch (Exception ex)
        {
            Append($"[Fehler: {ex.Message}]", Color.IndianRed);
        }
    }

    public void Stop()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.StandardInput.Close();
                if (!_process.WaitForExit(1500))
                    _process.Kill(entireProcessTree: true);
            }
        }
        catch { /* swallow on shutdown */ }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }

    private void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.SuppressKeyPress = true;

        var cmd = _input.Text;
        _input.Clear();

        if (_process is null || _process.HasExited)
        {
            Append("[Kein Prozess aktiv]", Color.IndianRed);
            return;
        }

        Append($"> {cmd}", Color.LightGreen);
        try { _process.StandardInput.WriteLine(cmd); }
        catch (Exception ex) { Append($"[Fehler: {ex.Message}]", Color.IndianRed); }
    }

    private void Append(string? text, Color color)
    {
        if (text is null) return;
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(() => Append(text, color))); return; }

        _output.SelectionStart = _output.TextLength;
        _output.SelectionLength = 0;
        _output.SelectionColor = color;
        _output.AppendText(text + Environment.NewLine);
        _output.SelectionColor = _output.ForeColor;
        _output.ScrollToCaret();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Stop();
        base.Dispose(disposing);
    }
}