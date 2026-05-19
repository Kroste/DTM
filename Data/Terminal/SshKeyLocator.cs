using Renci.SshNet;
using SystemFile = System.IO.File;

namespace DTM.Data.Terminal;

/// <summary>
/// Ergebnis von <see cref="SshKeyLocator.LoadKeys"/>. Erlaubt der aufrufenden
/// Seite, die drei Fälle "kein Verzeichnis", "keine ladbaren Keys obwohl
/// welche da sind" und "alles ok" zu unterscheiden — wichtig für aussagekräftige
/// Fehlermeldungen.
/// </summary>
public sealed record SshKeyLoadResult(
    IPrivateKeySource[] UsableKeys,
    string[] FoundFiles,
    string[] SkippedReasons)
{
    public bool HasUsable => UsableKeys.Length > 0;
    public bool HasAnyFiles => FoundFiles.Length > 0;
}

/// <summary>
/// Sucht private SSH-Schlüssel im Standard-Verzeichnis <c>~/.ssh</c>
/// in der gleichen Reihenfolge wie OpenSSH selbst.
/// </summary>
public static class SshKeyLocator
{
    // Reihenfolge wie OpenSSH: zuerst die modernen, dann RSA als Fallback.
    private static readonly string[] CandidateNames =
    {
        "id_ed25519",
        "id_ecdsa",
        "id_rsa"
    };

    public static IEnumerable<string> FindKeyFiles()
    {
        string sshDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh");
        if (!Directory.Exists(sshDir)) yield break;

        foreach (string name in CandidateNames)
        {
            string path = Path.Combine(sshDir, name);
            if (SystemFile.Exists(path)) yield return path;
        }
    }

    /// <summary>
    /// Lädt alle gefundenen Schlüssel als <see cref="IPrivateKeySource"/>.
    /// Wenn ein Key verschlüsselt ist, wird zuerst die in <see cref="SshRuntimeConfig"/>
    /// hinterlegte Passphrase versucht, dann ein Lade-Versuch ohne Passphrase.
    /// Pro Key wird höchstens ein Skip-Grund gemeldet.
    /// </summary>
    public static SshKeyLoadResult LoadKeys()
    {
        var usable  = new List<IPrivateKeySource>();
        var files   = new List<string>();
        var skips   = new List<string>();

        foreach (string path in FindKeyFiles())
        {
            string name = Path.GetFileName(path);
            files.Add(name);

            string? configPass = SshRuntimeConfig.GetPassphraseFor(name);

            // Strategie:
            //   1. Wenn eine Passphrase aus der Config bekannt ist → damit versuchen.
            //   2. Sonst zuerst ohne Passphrase. Wenn das fehlschlägt, ist der
            //      Key vermutlich verschlüsselt — sauberere Fehlermeldung an User.
            try
            {
                var key = configPass is null
                    ? new PrivateKeyFile(path)
                    : new PrivateKeyFile(path, configPass);
                usable.Add(key);
            }
            catch (Renci.SshNet.Common.SshPassPhraseNullOrEmptyException)
            {
                // Klassischer "Key ist verschlüsselt, aber keine Passphrase".
                skips.Add($"'{name}' übersprungen: verschlüsselt, aber keine Passphrase " +
                          "konfiguriert (siehe Ssh.KeyPassphrases in appsettings.json)");
            }
            catch (Exception ex) when (
                configPass is not null && (
                    ex.Message.Contains("check bytes", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("passphrase",  StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("decrypt",     StringComparison.OrdinalIgnoreCase)))
            {
                // Passphrase aus Config passt nicht zum Key. Klare Diagnose statt
                // kryptischer "random check bytes do not match"-Meldung.
                skips.Add($"'{name}' übersprungen: Passphrase aus appsettings.json wird vom Key abgelehnt");
            }
            catch (Exception ex)
            {
                // Unspezifischer Lese-/Format-Fehler.
                skips.Add($"'{name}' übersprungen: {ex.Message}");
            }
        }

        return new SshKeyLoadResult(
            UsableKeys: usable.ToArray(),
            FoundFiles: files.ToArray(),
            SkippedReasons: skips.ToArray());
    }
}
