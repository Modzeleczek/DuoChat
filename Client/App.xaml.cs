using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel;
using Shared;
using System.IO;
using System.Windows;

namespace Client
{
    public partial class App : ThemedApplication
    {
        public static string FileDialogInitialDirectory { get; private set; } = 
            Directory.GetCurrentDirectory();

        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(null!, new MainViewModel()).Show();
        }

        public App()
        {
            InitializeComponent();
        }

        public static void UpdateFileDialogInitialDirectory(string? path)
        {
            FileDialogInitialDirectory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        }
    }
}
