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
