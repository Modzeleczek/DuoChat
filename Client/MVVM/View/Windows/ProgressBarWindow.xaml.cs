using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class ProgressBarWindow : DialogWindow
    {
        public ProgressBarWindow(Window owner, ProgressBarViewModel dataContext)
            : base(owner, dataContext)
        { }

        protected override void Initialize() => InitializeComponent();
    }
}
