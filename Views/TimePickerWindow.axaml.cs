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
        var dt = (DataContext as TimePickerViewModel)?.ComposeDateTime() ?? DateTime.Now;
        Close(TimePickResult.At(dt));
    }

    private void OnImmediate(object? sender, RoutedEventArgs e)
    {
        Close(TimePickResult.Immediate());
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(TimePickResult.Cancel());
    }
}
