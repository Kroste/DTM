using Renci.SshNet;
using SystemFile = System.IO.File;

namespace DTM.Data.Terminal;

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
    /// Schlüssel mit Passphrase oder Lese-Fehlern werden übersprungen
    /// und über <paramref name="onSkipped"/> gemeldet.
    /// </summary>
    public static IPrivateKeySource[] LoadKeys(Action<string>? onSkipped = null)
    {
        var keys = new List<IPrivateKeySource>();
        foreach (string path in FindKeyFiles())
        {
            try
            {
                keys.Add(new PrivateKeyFile(path));
            }
            catch (Exception ex)
            {
                onSkipped?.Invoke($"SSH-Key '{Path.GetFileName(path)}' übersprungen: {ex.Message}");
            }
        }
        return keys.ToArray();
    }
}
