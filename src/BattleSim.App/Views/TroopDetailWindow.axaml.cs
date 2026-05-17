using Avalonia.Controls;
using Avalonia.Interactivity;
using BattleSim.App.ViewModels;

namespace BattleSim.App.Views;

public sealed partial class TroopDetailWindow : Window
{
    public TroopDetailWindow()
    {
        InitializeComponent();
    }

    public TroopDetailWindow(TroopDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
