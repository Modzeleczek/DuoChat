using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class ConfirmationWindow : DialogWindow
    {
        public ConfirmationWindow(Window owner, ConfirmationViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
