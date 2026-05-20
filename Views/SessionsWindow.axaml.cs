using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DTM.Views;

public partial class SessionsWindow : Window
{
    public SessionsWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnTitleClose(object? _, RoutedEventArgs e) => Close();
}
