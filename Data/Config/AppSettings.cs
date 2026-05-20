namespace DTM.Config;

public sealed class AppSettings
{
    public Dictionary<string, ServerConfig> Servers { get; set; } = new();

    /// <summary>
    /// Optionale SSH-Konfiguration. Wird vom <c>SshKeyLocator</c> beim Aufbau
    /// einer SSH-Verbindung konsultiert, um passphrase-geschützte Keys aus
    /// <c>~/.ssh</c> öffnen zu können.
    /// </summary>
    public SshConfig Ssh { get; set; } = new();

    /// <summary>
    /// Konfiguration rund um das FOC-SQL-PowerShell-Modul, über das DTM
    /// Backup/Clone/Snapshot etc. ausführt.
    /// </summary>
    public FocSqlConfig FocSql { get; set; } = new();
}

/// <summary>
/// Pfad zum FOC-SQL.psm1-Modul. DTM importiert dieses Modul in die pwsh-Session
/// und ruft dessen Funktionen (Backup-Database, Set-Snapshot, Sync-Database-ToTest)
/// auf, statt das Remoting selbst nachzubauen.
/// </summary>
public sealed class FocSqlConfig
{
    /// <summary>
    /// Voller Pfad zu einer FOC-SQL.psm1 (Override). Leer = DTM nutzt die
    /// Profil-Logik: Modul von <see cref="SambaSource"/> in den User-PSModulePath
    /// kopieren (falls nicht vorhanden) und per Namen importieren.
    /// </summary>
    public string ModulePath { get; set; } = string.Empty;

    /// <summary>
    /// Samba-Quelle (Glob) für die Profil-Logik. Wird nach
    /// $env:PSModulePath[0] kopiert. Leer = eingebauter Default.
    /// </summary>
    public string SambaSource { get; set; } = string.Empty;
}

public sealed class ServerConfig
{
    public string Server { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "Master";

    public ServerCredential ToCredential()
        => new(Server, User, Password, Database);
}

/// <summary>
/// SSH-Optionen. Die <see cref="KeyPassphrases"/>-Map ordnet Key-Dateinamen
/// (ohne Pfad, z.B. "id_rsa", "id_ed25519") ihrer Passphrase zu.
/// Eine zusätzliche generische Passphrase unter dem Schlüssel <c>"*"</c>
/// gilt als Fallback für alle Keys, deren konkreter Eintrag fehlt.
/// </summary>
public sealed class SshConfig
{
    public Dictionary<string, string> KeyPassphrases { get; set; } = new();
}
