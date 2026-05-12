namespace DTM
{
    public class cTreeViewNodeParent : TreeNode
    {
        public DB_SERVER.ServerTyp ServerTyp {get;private set;}
        public cTreeViewNodeParent(string? Text, DB_SERVER.ServerTyp serverTyp):base(Text)
        {
            this.ServerTyp = serverTyp;
        }
    }
    public class cTreeViewNodeDatabase : TreeNode
    {
        public Database_Info Database {get;private set;}
        public cTreeViewNodeDatabase(string? Text, Database_Info Database):base(Text)
        {
            this.Database = Database;
        }
    }
}