namespace DTM
{
    public class ServerCredential(string Server = "FOC-SQL01", string User = "", string Password = "", string Datenbank = "Master", string ConnectionString = "")
    {
        public string Server { get; set; } = Server;
        public string User { get; set; } = User;
        public string Password { get; set; } = Password;
        public string Datenbank { get; set; } = Datenbank;
        public string ConnectionString { get; set; } = ConnectionString;
    }
}