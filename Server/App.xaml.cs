using Server.MVVM.View.Windows;
using Server.MVVM.ViewModel;
using Shared;
using System.Windows;

namespace Server
{
    public partial class App : ThemedApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(null!, new MainViewModel()).Show();
        }

        public App()
        {
            InitializeComponent();
        }
    }
}
