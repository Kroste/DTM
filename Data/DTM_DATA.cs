using NLog;

namespace DTM
{
    public class DTM_DATA : IDTM_DATA
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public Dictionary<DB_SERVER.ServerTyp, DB_SERVER> db_Servers { get; private set; }
        private readonly IODBC_Factory _factory;

        public DTM_DATA(Dictionary<DB_SERVER.ServerTyp, DB_SERVER> dB_SERVERs, IODBC_Factory factory)
        {
            this.db_Servers = dB_SERVERs;
            this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public List<Database_Info> get_Database_Names(DB_SERVER.ServerTyp serverTyp)
        {
            _logger.Debug("get_Database_Names: ServerTyp={0}", serverTyp);
            try
            {
                var result = _factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.get_Datenbank_Names();
                _logger.Info("get_Database_Names: {0} Datenbanken geladen.", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "get_Database_Names fehlgeschlagen: ServerTyp={0}", serverTyp);
                throw;
            }
        }

        public Database_Stats get_Database_Stats(DB_SERVER.ServerTyp serverTyp, Database_Info database)
        {
            _logger.Debug("get_Database_Stats: ServerTyp={0}, Datenbank={1}", serverTyp, database.Name);
            try
            {
                var result = _factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.GetDatabase_Stats(database);
                _logger.Info("get_Database_Stats: Stats für '{0}' geladen.", database.Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "get_Database_Stats fehlgeschlagen: ServerTyp={0}, Datenbank={1}", serverTyp, database.Name);
                throw;
            }
        }
    }
}
