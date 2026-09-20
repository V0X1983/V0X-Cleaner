using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class HomePageView : UserControl
{
    public HomePageView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is HomePageViewModel vm && vm.RefreshCommand.CanExecute(null))
            {
                vm.RefreshCommand.Execute(null);
            }
        };
    }
}
