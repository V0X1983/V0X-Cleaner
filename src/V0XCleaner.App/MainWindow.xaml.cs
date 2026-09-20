using System.Windows;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
