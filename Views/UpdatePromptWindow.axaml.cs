using Avalonia.Interactivity;

namespace DTM.Views;

public enum UpdateDialogResult { ApplyNow, Later, Skip }

public partial class UpdatePromptWindow : ChromeWindow
{
    public UpdateDialogResult Result { get; private set; } = UpdateDialogResult.Skip;

    public UpdatePromptWindow() => InitializeComponent();

    public UpdatePromptWindow(string newVersion, string currentVersion)
    {
        InitializeComponent();
        MessageText.Text =
            $"Version {newVersion} ist verfügbar (aktuell: {currentVersion}).\n" +
            "Jetzt aktualisieren?";
    }

    private void OnTitleClose(object? _, RoutedEventArgs e) => Close();

    private void OnApply(object? _, RoutedEventArgs e) { Result = UpdateDialogResult.ApplyNow; Close(); }
    private void OnLater(object? _, RoutedEventArgs e) { Result = UpdateDialogResult.Later;    Close(); }
    private void OnSkip (object? _, RoutedEventArgs e) { Result = UpdateDialogResult.Skip;     Close(); }
}
