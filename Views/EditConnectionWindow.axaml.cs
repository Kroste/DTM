using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DTM.Views;

public partial class EditConnectionWindow : Window
{
    public EditConnectionWindow()
    {
        InitializeComponent();
    }

    private void OnSave(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
