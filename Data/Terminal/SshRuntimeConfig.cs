using DTM.Config;

namespace DTM.Data.Terminal;

/// <summary>
/// Brücke zwischen den geladenen <see cref="SshConfig"/>-Daten und den
/// SSH-Klassen, die nicht über DI angebunden sind (<c>SshKeyLocator</c>,
/// <c>SshTerminalSession</c>). Wird einmal beim App-Start gesetzt.
/// </summary>
public static class SshRuntimeConfig
{
    private static SshConfig _config = new();

    public static SshConfig Current
    {
        get => _config;
        set => _config = value ?? new SshConfig();
    }

    /// <summary>
    /// Sucht die Passphrase für einen Key-Dateinamen (ohne Pfad). Liefert
    /// erst den exakten Match, fällt dann auf <c>"*"</c> zurück, sonst <c>null</c>.
    /// </summary>
    public static string? GetPassphraseFor(string keyFileName)
    {
        if (_config.KeyPassphrases.TryGetValue(keyFileName, out var p) && !string.IsNullOrEmpty(p))
            return p;
        if (_config.KeyPassphrases.TryGetValue("*", out var fallback) && !string.IsNullOrEmpty(fallback))
            return fallback;
        return null;
    }
}
