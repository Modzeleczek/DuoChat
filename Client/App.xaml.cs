using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel;
using Shared;
using System.Windows;

namespace Client
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
