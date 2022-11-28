using Client.MVVM.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Client.MVVM.View.Windows
{
    public partial class SettingsWindow : DialogWindow
    {
        public SettingsWindow(Window owner, SettingsViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();

        private void Button_Close_Click( object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
