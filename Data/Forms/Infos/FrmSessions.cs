using NLog;
namespace DTM
{
    public class FrmSessions : Form
    {
        private DataGridView dataGridView = null!;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        public FrmSessions()
        {
            InitializeComponent();
        }

        private void FrmSessions_Load(object sender, EventArgs e)
        {
            _logger.Debug("FrmSessions geladen.");
        }

        private
        void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FrmSessions
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "FrmSessions";
            this.Text = "Sessions";
            this.ResumeLayout(false);

            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
              this.Controls.Add(dataGridView);
        }

        public void SetSessionsData(List<Session> dataSource)
        {
            dataGridView.DataSource = dataSource;
        }
    }
}