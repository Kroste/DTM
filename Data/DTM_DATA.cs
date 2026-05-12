namespace DTM
{
    public class DTM_DATA : IDTM_DATA
    {
        public Dictionary<DB_SERVER.ServerTyp, DB_SERVER> db_Servers { get; private set; }

        public DTM_DATA(Dictionary<DB_SERVER.ServerTyp, DB_SERVER> dB_SERVERs)
        {
            this.db_Servers = dB_SERVERs;
        }
        public List<Database_Info> get_Database_Names(DB_SERVER.ServerTyp serverTyp)
        {
            return ODBC_Factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.get_Datenbank_Names();
        }

        public Database_Stats get_Database_Stats(DB_SERVER.ServerTyp serverTyp, Database_Info database)
        {
            return ODBC_Factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.GetDatabase_Stats(database);
        }

        public bool Backup_Database(DB_SERVER.ServerTyp serverTyp, Database_Info Database, DateTime backupTime)
        {
            return ODBC_Factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.Backup_Database(Database, backupTime);
        }

        public bool Clone_Database(DB_SERVER.ServerTyp serverTyp, Database_Info Database, DateTime cloneTime)
        {
            return ODBC_Factory.Get_DATA(serverTyp.ToString(), db_Servers.FirstOrDefault(x => x.Key == serverTyp).Value.serverCredential!)!.Clone_Database(Database, cloneTime);
        }
    }
}