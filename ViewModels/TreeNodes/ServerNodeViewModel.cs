using Avalonia.Threading;
using NLog;

namespace DTM.ViewModels.TreeNodes;

public sealed class ServerNodeViewModel : NodeViewModelBase
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly IDTM_DATA _data;
    private bool _childrenLoaded;

    public DB_SERVER.ServerTyp ServerTyp { get; }

    public ServerNodeViewModel(DB_SERVER.ServerTyp typ, IDTM_DATA data)
    {
        ServerTyp = typ;
        _data = data;
        Header = typ.ToString();
    }

    protected override void OnExpanded()
    {
        _ = EnsureChildrenLoadedAsync();
    }

    public async Task EnsureChildrenLoadedAsync()
    {
        if (_childrenLoaded) return;
        _childrenLoaded = true;
        await LoadChildrenAsync();
    }

    private async Task LoadChildrenAsync()
    {
        IsLoading = true;
        try
        {
            var dbs = await Task.Run(() => _data.get_Database_Names(ServerTyp));
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Children.Clear();
                foreach (var db in dbs)
                {
                    Children.Add(new DatabaseNodeViewModel(db, ServerTyp));
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Fehler beim Laden der DBs für {ServerTyp}.");
            _childrenLoaded = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
