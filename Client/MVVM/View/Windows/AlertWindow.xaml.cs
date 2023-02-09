using Client.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class AlertWindow : DialogWindow
    {
        public AlertWindow(Window owner, AlertViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
