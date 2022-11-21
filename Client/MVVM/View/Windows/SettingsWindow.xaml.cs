using Client.MVVM.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Client.MVVM.View.Windows
{
    public partial class SettingsWindow : DialogWindow
    {
        protected override void Initialize() => InitializeComponent();

        public SettingsWindow(Window owner, DialogViewModel dataContext)
            : base(owner, dataContext) { }

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
