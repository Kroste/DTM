namespace DTM
{
    public class DTM_DATA : IDTM_DATA
    {
        public Dictionary<DB_SERVER.ServerTyp, DB_SERVER> db_Servers { get; private set; }
        private readonly IODBC_Factory _factory;

        public DTM_DATA(Dictionary<DB_SERVER.ServerTyp, DB_SERVER> dB_SERVERs, IODBC_Factory factory)
        {
            this.db_Servers = dB_SERVERs;
            this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        public List<Database_Info> get_Database_Names(DB_SERVER.ServerTyp serverTyp)
        {
            return _factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.get_Datenbank_Names();
        }

        public Database_Stats get_Database_Stats(DB_SERVER.ServerTyp serverTyp, Database_Info database)
        {
            return _factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.GetDatabase_Stats(database);
        }
    }
}
