namespace DTM
{
    public interface IDTM_DATA
    {
        public List<Database_Info> get_Database_Names(DB_SERVER.ServerTyp serverTyp);
        public Database_Stats get_Database_Stats(DB_SERVER.ServerTyp serverTyp, Database_Info database);
    }
}
