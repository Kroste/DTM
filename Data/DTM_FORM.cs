using NLog;

namespace DTM
{
    public class DTM_FORM : IDTM_FORM, IDisposable
    {
        private readonly IDTM_DATA _data;
        private readonly TreeView mainView;
        private readonly Dictionary<DB_SERVER.ServerTyp, DB_SERVER> dB_SERVERs;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        public DTM_FORM(Dictionary<DB_SERVER.ServerTyp, DB_SERVER> db_Servers, TreeView mainView)
        {
            this.dB_SERVERs = db_Servers;
            this.mainView = mainView;
            _data = new DTM_DATA(db_Servers);
            InitializeComponent();

        }

        private void InitializeComponent()
        {
            new Thread(new ThreadStart(() =>
             {
                 mainView.InvokeIfRequired(() =>
                 {
                     mainView.Nodes.Clear();
                     mainView.BeginUpdate();
                     foreach (KeyValuePair<DB_SERVER.ServerTyp, DB_SERVER> dB_SERVER in dB_SERVERs)
                     {
                         mainView.Nodes.Add(new cTreeViewNodeParent(dB_SERVER.Key.ToString(), dB_SERVER.Key));
                     }
                     mainView.EndUpdate();
                 });
             })).Start();
        }

        public void get_Database_Names()
        {
            cTreeViewNodeParent? parentNode = mainView.SelectedNode as cTreeViewNodeParent;

            if (parentNode != null)
            {
                new Thread(new ThreadStart(() =>
                {
                    mainView.InvokeIfRequired(() =>
                    {
                        parentNode.Nodes.Clear();
                        mainView.BeginUpdate();
                        foreach (Database_Info db_info in _data.get_Database_Names(parentNode.ServerTyp))
                        {
                            _ = parentNode.Nodes.Add(new cTreeViewNodeDatabase($"{db_info.Name} ({db_info.Status})", db_info));
                        }
                        mainView.EndUpdate();
                    });
                })).Start();

            }
        }

        public Database_Stats? get_Database_Stats()
        {
            cTreeViewNodeDatabase? childNode = mainView.SelectedNode as cTreeViewNodeDatabase;

            if (childNode != null)
            {
                cTreeViewNodeParent? parentNode = mainView.SelectedNode?.Parent as cTreeViewNodeParent;

                if (parentNode != null)
                {
                    Database_Stats _stats = _data.get_Database_Stats(parentNode.ServerTyp, childNode.Database);
                    if (_stats != null)
                    {
                        _logger.Debug($"Stats: {_stats}");
                        return _stats;

                    }
                }
            }
            return null;
        }

        public bool Backup_Database(DateTime backupTime)
        {
            cTreeViewNodeDatabase? childNode = mainView.SelectedNode as cTreeViewNodeDatabase;

            if (childNode != null)
            {
                cTreeViewNodeParent? parentNode = mainView.SelectedNode?.Parent as cTreeViewNodeParent;

                if (parentNode != null)
                {                 
                    _data.Backup_Database(parentNode.ServerTyp, childNode.Database, backupTime);
                       _logger.Info($"Backup für {childNode.Database.Name} um {backupTime} gestartet.");
                }
            }
            return true;
        }

        public bool Clone_Database(DateTime cloneTime)
        {
            cTreeViewNodeDatabase? childNode = mainView.SelectedNode as cTreeViewNodeDatabase;

            if (childNode != null)
            {
                cTreeViewNodeParent? parentNode = mainView.SelectedNode?.Parent as cTreeViewNodeParent;

                if (parentNode != null)
                {                                     
                    _data.Clone_Database(parentNode.ServerTyp, childNode.Database, cloneTime);
                    _logger.Info($"Klonen der Datenbank {childNode.Database.Name} um {cloneTime} gestartet.");
                }
            }
            return true;
        }

        public void Dispose()
        {
        }
    }
}