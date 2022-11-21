using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public class DialogWindow : Window
    {
        public DialogWindow() { }

        protected DialogWindow(Window owner, DialogViewModel dataContext)
        {
            Initialize();
            DataContext = dataContext;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        protected virtual void Initialize() { }
    }
}
