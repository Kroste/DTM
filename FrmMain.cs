using NLog;
using System.Collections.Generic;
namespace DTM
{
    public partial class Main_Form : Form
    {
        private IDTM_FORM dtm_form;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        public Main_Form()
        {
            InitializeComponents();
            WireEvents();
            Dictionary<DB_SERVER.ServerTyp, DB_SERVER> dB_SERVERs = new Dictionary<DB_SERVER.ServerTyp, DB_SERVER>
            {
                { DB_SERVER.ServerTyp.MSSQL, new DB_SERVER(new ServerCredential(User: "uOsteL", Password: "London\"&Hga28f")) },
                { DB_SERVER.ServerTyp.ORACLE, new DB_SERVER(new ServerCredential(User:"lars@internal",Password:"London22Hga28f", Server:"olvm-mgmt.lhp.intern")) }
            };
            dtm_form = new DTM_FORM(dB_SERVERs, mainView);
        }


        private void mainView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _logger.Debug($"mainView Node Klickt: {e.Node?.Text}");

            if (e.Node?.Parent == null)
            {
                dtm_form.get_Database_Names();
            }
            else
            {
                Database_Stats_MSSQL? _statsMSSQL = dtm_form.get_Database_Stats() as Database_Stats_MSSQL;

                if (_statsMSSQL != null)
                {
                    lblDbName.Text = $"Datenbank: {_statsMSSQL.Name ?? "—"}";
                    lblDbHost.Text = $"Host: {_statsMSSQL.Server}" ?? "—";
                    lblDbStatus.Text = $"Status: {_statsMSSQL.State}" ?? "—";
                    lblDbVersion.Text = $"Comp. Lvl.: {_statsMSSQL.CompatibllityLevel}" ?? "—";
                    lblDbSize.Text = $"Größe: {_statsMSSQL.DataSizeMB} MB" ?? "—";
                    lbRecoveryModel.Text = $"RecoveryModel: {_statsMSSQL.RecorveryModel}" ?? "—";
                    lbActiveSessions.Text = $"Aktive Sessions: {_statsMSSQL.Sessions?.Count()}" ?? "—";

                    tabPowerShell.Focus();
                }


                Database_Stats_ORACLE? _statsOracle = dtm_form.get_Database_Stats() as Database_Stats_ORACLE;

                if (_statsOracle != null)
                {
                    lblDbName.Text = $"Instance: {_statsOracle.InstanceName?? "—"}";
                    lblDbHost.Text = $"Host: {_statsOracle.Server}" ?? "—";
                    lblDbStatus.Text = $"Status: {_statsOracle.State}" ?? "—";
                    lblDbVersion.Text = $"Version: {_statsOracle.OracleVersion}" ?? "—";
                    lblDbSize.Text = $"Größe: {_statsOracle.DataSizeMB} MB" ?? "—";
                    lbRecoveryModel.Text = $"ArchiveLog: {_statsOracle.ArchiveLogMode}" ?? "—";
                    lbActiveSessions.Text = $"Aktive Sessions: {_statsOracle.Sessions?.Count()}" ?? "—";

                    tabSsh.Focus();
                }
            }
        }
    }
}