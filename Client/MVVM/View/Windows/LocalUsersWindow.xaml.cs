using Client.MVVM.ViewModel.LocalUsers;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class LocalUsersWindow : DialogWindow
    {
        public LocalUsersWindow(Window owner, LocalUsersViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
