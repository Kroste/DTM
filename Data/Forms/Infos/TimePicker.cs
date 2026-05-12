using NLog;
namespace DTM
{
    public class FrmTimePicker : Form
    {
        private DateTimePicker? dateTimePicker = null!;
        private Button? btnOK = null!;
        private Button? btnCancel = null!;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public FrmTimePicker()
        {
            InitializeComponent();
        }

        private void FrmTimePicker_Load(object sender, EventArgs e)
        {
            _logger.Debug("FrmTimePicker geladen.");
        }

        private
        void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FrmTimePicker
            // 
            this.ClientSize = new System.Drawing.Size(250, 100);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "TimePicker";
            this.Text = "TimePicker";
            this.ResumeLayout(false);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            dateTimePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dddd.MMMM.yyyy HH:mm",
                ShowUpDown = false,
                MinDate = DateTime.Now,
                Value = DateTime.Now
            };
            Controls.Add(dateTimePicker);

            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Location = new Point(30, 60),
                Size = new Size(20, 30)                
            };

            btnCancel = new Button
            {
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Bottom,
                Location = new Point(120, 60),
                Size = new Size(20, 30)
            };

            AcceptButton = btnOK;
            CancelButton = btnCancel;   
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
        }

        public DateTime GetDateTime()
        {
            return dateTimePicker.Value;
        }
    }
}