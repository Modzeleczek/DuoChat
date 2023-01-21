using Server.MVVM.View.Windows;
using Server.MVVM.ViewModel;
using System.Windows;

namespace Server
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(null, new MainViewModel()).Show();
        }
    }
}
