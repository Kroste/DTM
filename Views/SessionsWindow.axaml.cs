using Avalonia.Interactivity;

namespace DTM.Views;

public partial class SessionsWindow : ChromeWindow
{
    public SessionsWindow()
    {
        InitializeComponent();
    }

    private void OnTitleClose(object? _, RoutedEventArgs e) => Close();
}
