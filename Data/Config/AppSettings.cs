namespace DTM.Config;

public sealed class AppSettings
{
    public Dictionary<string, ServerConfig> Servers { get; set; } = new();
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
