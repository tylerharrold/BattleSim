using Avalonia.Controls;
using BattleSim.App.ViewModels;

namespace BattleSim.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
