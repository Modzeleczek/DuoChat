using Client.MVVM.Model;
using System.IO;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var path = DataAccessObject.DATABASES_DIRECTORY_PATH;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
