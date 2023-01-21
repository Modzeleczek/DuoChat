using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel;
using System.IO;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var path = LocalUsersStorage.USERS_DIRECTORY_PATH;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            new MainWindow(null, new MainViewModel()).Show();
        }
    }
}
