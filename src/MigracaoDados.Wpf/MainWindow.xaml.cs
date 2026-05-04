using MigracaoDados.Wpf.ViewModels;

namespace MigracaoDados.Wpf;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}