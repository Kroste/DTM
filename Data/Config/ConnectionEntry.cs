using System.Text.Json.Serialization;

namespace DTM.Config;

public sealed class ConnectionEntry
{
    public string Key { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string PasswordProtected { get; set; } = string.Empty;
    public string Database { get; set; } = "Master";
    public string ConnectionString { get; set; } = string.Empty;

    [JsonIgnore]
    public string PlainPassword
    {
        get => ConnectionStore.Unprotect(PasswordProtected);
        set => PasswordProtected = ConnectionStore.Protect(value);
    }

    public ServerCredential ToCredential() =>
        new(Server, User, PlainPassword, Database, ConnectionString);
}
