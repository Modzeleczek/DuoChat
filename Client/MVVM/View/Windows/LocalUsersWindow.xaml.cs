using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class LocalUsersWindow : DialogWindow
    {
        protected override void Initialize() => InitializeComponent();

        public LocalUsersWindow(Window owner, DialogViewModel dataContext)
            : base(owner, dataContext) { }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Closing -= Window_Closing;
            Close();
        }

        private void Button_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
