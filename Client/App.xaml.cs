using Client.MVVM.Model.BsonStorages;
using System.IO;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var path = LocalUsersStorage.USERS_DIRECTORY_PATH;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
