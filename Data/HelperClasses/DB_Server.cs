namespace DTM
{
    public class DB_SERVER
    {
        public enum ServerTyp
        {
            ORACLE,
            MSSQL,
            PostgreSQL
        }

        public ServerTyp Typ { get; }
        public ServerCredential? serverCredential { get; private set; }

        /// <summary>
        /// Composite-Identitaet (Typ, Hostname). Wird in Phase 6 zur eindeutigen
        /// Adressierung eines Servers genutzt — frueher reichte der Typ allein
        /// (Dictionary-Key), jetzt koennen mehrere Hosts pro Typ existieren.
        /// </summary>
        public ServerIdentity Identity =>
            new(Typ, serverCredential?.Server ?? string.Empty);

        public DB_SERVER(ServerTyp typ, ServerCredential serverCredential)
        {
            this.Typ = typ;
            this.serverCredential = serverCredential;
        }
    }
}