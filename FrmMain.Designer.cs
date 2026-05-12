namespace DTM;

using System.Windows.Forms;

partial class Main_Form
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    // Splitter
    private SplitContainer mainSplit = null!;
    private SplitContainer rightSplit = null!;
    private SplitContainer bottomSplit = null!;

    // Treeview (links)
    private TreeView mainView = null!;

    // Info-Panel (oben rechts)
    private Panel infoPanel = null!;
    private Label lblDbName = null!;
    private Label lblDbHost = null!;
    private Label lblDbStatus = null!;
    private Label lblDbVersion = null!;
    private Label lblDbSize = null!;

    // Action-Buttons
    private FlowLayoutPanel actionPanel = null!;
    private Button btnBackup = null!;
    private Button btnClone = null!;
    private Button btnSnapshot = null!;
    private Button btnDbToSamba = null!;

    // Console-Tabs (unten rechts)
    private TabControl consoleTabs = null!;
    private TabPage tabPowerShell = null!;
    private TabPage tabSsh = null!;
    private ConsoleControl powershellConsole = null!;
    private ConsoleControl sshConsole = null!;

    // Statusbar
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;


    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponents()
    {
        // ---------- Form ----------
        Text = "Datenbank-Manager";
        Size = new Size(1280, 760);
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // ---------- StatusStrip (Dock Bottom) ----------
        statusLabel = new ToolStripStatusLabel("Bereit");
        statusStrip = new StatusStrip();
        statusStrip.Items.Add(statusLabel);

        // ---------- Treeview ----------
        mainView = new TreeView
        {
            Name = "mainView",
            Dock = DockStyle.Fill,
            HideSelection = false,
            BorderStyle = BorderStyle.None
        };
        

        // ---------- Info-Panel (oben rechts) ----------
        infoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

        lblDbName = MakeLabel("Datenbank: —", 18f, FontStyle.Bold, new Point(15, 15));
        lblDbHost = MakeLabel("Host: —", 11f, FontStyle.Regular, new Point(15, 60));
        lblDbStatus = MakeLabel("Status: —", 11f, FontStyle.Regular, new Point(15, 85));
        lblDbVersion = MakeLabel("Version: —", 11f, FontStyle.Regular, new Point(15, 110));
        lblDbSize = MakeLabel("Größe: —", 11f, FontStyle.Regular, new Point(15, 135));

        infoPanel.Controls.AddRange(new Control[]
            { lblDbName, lblDbHost, lblDbStatus, lblDbVersion, lblDbSize });

        // ---------- Action-Buttons (mittig) ----------
        actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10),
            BackColor = SystemColors.Control,
            WrapContents = false,
            AutoScroll = true
        };

        btnBackup = MakeActionButton("Backup");
        btnClone = MakeActionButton("Clone");
        btnSnapshot = MakeActionButton("Snapshot");
        btnDbToSamba = MakeActionButton("DB > Samba");

        actionPanel.Controls.AddRange(new Control[]
            { btnBackup, btnClone, btnSnapshot, btnDbToSamba });

        // ---------- Console-Tabs ----------
        powershellConsole = new ConsoleControl
        {
            Dock = DockStyle.Fill,
            FileName = "powershell.exe",
            Arguments = "-NoLogo -NoExit"
            // Für PowerShell 7: FileName = "pwsh.exe"
        };

        sshConsole = new ConsoleControl
        {
            Dock = DockStyle.Fill,
            FileName = "ssh.exe",
            // Args später setzen wenn Host bekannt, z.B.:
            // Arguments = "-tt user@oraclelinux01"
            Arguments = ""
        };

        tabPowerShell = new TabPage("PowerShell (Windows)");
        tabPowerShell.Controls.Add(powershellConsole);

        tabSsh = new TabPage("SSH / Bash (Oracle Linux)");
        tabSsh.Controls.Add(sshConsole);

        consoleTabs = new TabControl { Dock = DockStyle.Fill };
        consoleTabs.TabPages.Add(tabPowerShell);
        consoleTabs.TabPages.Add(tabSsh);

        // ---------- Bottom-Split (Buttons / Console) ----------
        bottomSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.Panel1,
        };
        bottomSplit.Panel1.Controls.Add(actionPanel);
        bottomSplit.Panel2.Controls.Add(consoleTabs);

        // ---------- Right-Split (Info / Bottom) ----------
        rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
        };
        rightSplit.Panel1.Controls.Add(infoPanel);
        rightSplit.Panel2.Controls.Add(bottomSplit);

        // ---------- Main-Split (Tree / Right) ----------
        mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
        };
        mainSplit.Panel1.Controls.Add(mainView);
        mainSplit.Panel2.Controls.Add(rightSplit);

        // ---------- In Form einhängen (Reihenfolge wichtig!) ----------
        // Erst dockende Controls (StatusStrip), dann Fill-Container
        Controls.Add(mainSplit);
        Controls.Add(statusStrip);
    }

    private void WireEvents()
    {
        // Buttons (Stubs — Logik kommt später)
        btnBackup.Click += (_, _) => statusLabel.Text = "Backup gestartet…";
        btnClone.Click += (_, _) => statusLabel.Text = "Clone gestartet…";
        btnSnapshot.Click += (_, _) => statusLabel.Text = "Snapshot gestartet…";
        btnDbToSamba.Click += (_, _) => statusLabel.Text = "DB → Samba gestartet…";

        mainView.AfterSelect += (_, e) =>
        {
            // Hier später: Labels mit Daten der ausgewählten DB füllen            
            mainView_AfterSelect(_,e);
        };        

        Shown += (_, _) =>
        {
            SetSplitterSafe(mainSplit, 260);
            SetSplitterSafe(rightSplit, 220);
            SetSplitterSafe(bottomSplit, 80);

            mainSplit.Panel1MinSize = 150;
            mainSplit.Panel2MinSize = 400;
            rightSplit.Panel1MinSize = 120;
            rightSplit.Panel2MinSize = 200;
            bottomSplit.Panel1MinSize = 70;
            bottomSplit.Panel2MinSize = 100;
        };
        // Konsole(n) starten — PowerShell sofort, SSH erst on-demand
        Load += (_, _) => powershellConsole.Start();
        FormClosing += (_, _) => { powershellConsole.Stop(); sshConsole.Stop(); };
    }

    // === Helper ===

    private static Label MakeLabel(string text, float size, FontStyle style, Point loc) => new()
    {
        Text = text,
        AutoSize = true,
        Location = loc,
        Font = new Font("Segoe UI", size, style)
    };

    private static Button MakeActionButton(string text) => new()
    {
        Text = text,
        Size = new Size(120, 50),
        BackColor = Color.Gold,
        ForeColor = Color.Black,
        FlatStyle = FlatStyle.Flat,             // wichtig, sonst ignoriert Win11 die BackColor
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
        Margin = new Padding(6),
        UseVisualStyleBackColor = false
    };

    private static void SetSplitterSafe(SplitContainer sc, int desired)
    {
        // Erst Layout erzwingen, damit Width/Height aktuell sind
        sc.PerformLayout();

        int total = sc.Orientation == Orientation.Vertical ? sc.Width : sc.Height;
        int min = sc.Panel1MinSize;
        int max = total - sc.Panel2MinSize - sc.SplitterWidth;

        if (max <= min) return; // Container zu klein — überspringen statt crashen

        sc.SplitterDistance = Math.Clamp(desired, min, max);
    }

    #endregion
}
