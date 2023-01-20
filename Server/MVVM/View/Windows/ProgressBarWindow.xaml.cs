using Server.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Server.MVVM.View.Windows
{
    public partial class ProgressBarWindow : DialogWindow
    {
        public ProgressBarWindow(Window owner, ProgressBarViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
