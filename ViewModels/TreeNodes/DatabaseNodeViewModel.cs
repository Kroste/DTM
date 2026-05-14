namespace DTM.ViewModels.TreeNodes;

public sealed class DatabaseNodeViewModel : NodeViewModelBase
{
    public Database_Info Database { get; }
    public DB_SERVER.ServerTyp ServerTyp { get; }

    public DatabaseNodeViewModel(Database_Info info, DB_SERVER.ServerTyp typ)
    {
        Database = info;
        ServerTyp = typ;
        Header = $"{info.Name} ({info.Status})";
    }
}
