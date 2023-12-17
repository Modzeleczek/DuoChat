using Shared.MVVM.ViewModel;
using System.Windows;

namespace Shared.MVVM.View.Windows
{
    public partial class ConfirmationWindow : DialogWindow
    {
        public ConfirmationWindow(Window owner, ConfirmationViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
