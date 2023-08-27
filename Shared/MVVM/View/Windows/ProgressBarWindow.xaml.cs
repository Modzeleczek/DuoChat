using Shared.MVVM.ViewModel.LongBlockingOperation;
using System.Windows;

namespace Shared.MVVM.View.Windows
{
    public partial class ProgressBarWindow : DialogWindow
    {
        public ProgressBarWindow(Window owner, ProgressBarViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
