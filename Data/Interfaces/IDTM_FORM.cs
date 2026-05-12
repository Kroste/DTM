namespace DTM
{
    public interface IDTM_FORM
    {
        public void get_Database_Names();
        public Database_Stats? get_Database_Stats();
        bool Clone_Database(DateTime cloneTime);
        bool Backup_Database(DateTime backupTime);
    }
}