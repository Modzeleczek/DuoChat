using Server.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Server.MVVM.View.Windows
{
    public partial class ConfirmationWindow : DialogWindow
    {
        public ConfirmationWindow(Window owner, ConfirmationViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
