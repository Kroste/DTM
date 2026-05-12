namespace DTM
{
    public class ServerCredential
    {
        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Datenbank { get; set; }
        public ServerCredential(string Server = "FOC-SQL01", string User = "", string Password = "", string Datenbank = "Master")
        {
            this.Server = Server;
            this.Password = Password;
            this.User = User;
            this.Datenbank = Datenbank;
        }
    }
}