namespace DTM
{
    public class DB_SERVER
    {
        public enum ServerTyp
        {
            ORACLE,
            MSSQL
        }
        public ServerCredential? serverCredential { get; private set; }
        public DB_SERVER(ServerCredential serverCredential)
        {
            this.serverCredential = serverCredential;
        }
    }
}