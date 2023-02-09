using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(null, new MainViewModel()).Show();
        }
    }
}
