namespace DTM.ODBC
{
    public interface IDTM_ODBC
    {
        public List<Database_Info> get_Datenbank_Names();
        public Database_Stats GetDatabase_Stats(Database_Info database);
        bool Backup_Database(Database_Info Database, DateTime backupTime);
        bool Clone_Database(Database_Info Database, DateTime backupTime);
    }
}