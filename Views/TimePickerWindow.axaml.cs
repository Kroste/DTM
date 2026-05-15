using Avalonia.Controls;
using Avalonia.Interactivity;
using DTM.ViewModels;

namespace DTM.Views;

public partial class TimePickerWindow : Window
{
    public TimePickerWindow()
    {
        InitializeComponent();
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        var result = (DataContext as TimePickerViewModel)?.ComposeDateTime();
        Close(result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close((DateTime?)null);
    }
}
