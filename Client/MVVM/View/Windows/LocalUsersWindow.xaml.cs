using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class LocalUsersWindow : DialogWindow
    {
        protected override void Initialize() => InitializeComponent();

        public LocalUsersWindow(Window owner, DialogViewModel dataContext)
            : base(owner, dataContext) { }
    }
}
