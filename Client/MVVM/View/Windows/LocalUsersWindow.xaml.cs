using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class LocalUsersWindow : DialogWindow
    {
        public LocalUsersWindow(Window owner, LocalUsersViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
