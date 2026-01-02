using Praxis.ViewModels;
using System.Windows;

namespace Praxis;

/// <summary>
/// Main application window for Praxis.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
