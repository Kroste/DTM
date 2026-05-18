using System.Collections.ObjectModel;

namespace DTM.ViewModels;

public sealed class SessionsViewModel : ViewModelBase
{
    public ObservableCollection<Session> Sessions { get; } = new();

    public void SetSessions(IEnumerable<Session>? sessions)
    {
        Sessions.Clear();
        if (sessions is null) return;
        foreach (Session s in sessions)
        {
            Sessions.Add(s);
        }
    }
}
