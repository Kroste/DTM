namespace DTM
{
    public interface IDTM_DATA
    {
        /// <summary>
        /// Alle registrierten Server-Verbindungen. Wird vom Tree-Aufbau im
        /// MainWindowViewModel iteriert (Phase 6: ein Knoten pro Server,
        /// gruppiert nach Typ).
        /// </summary>
        IReadOnlyList<DB_SERVER> Servers { get; }

        /// <summary>
        /// Datenbank-Liste eines konkreten Servers (identifiziert ueber
        /// <see cref="ServerIdentity"/>, also Typ + Hostname).
        /// </summary>
        List<Database_Info> get_Database_Names(ServerIdentity identity);

        /// <summary>
        /// Statistiken einer Datenbank auf einem konkreten Server.
        /// </summary>
        Database_Stats get_Database_Stats(ServerIdentity identity, Database_Info database);
    }
}
