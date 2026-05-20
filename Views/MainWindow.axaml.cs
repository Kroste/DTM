using Avalonia.Controls;
using DTM.ViewModels;

namespace DTM.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                await vm.CheckForUpdateAsync();
        };
    }
}
