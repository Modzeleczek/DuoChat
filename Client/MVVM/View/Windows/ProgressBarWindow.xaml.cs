using Client.MVVM.ViewModel;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class ProgressBarWindow : DialogWindow
    {
        public ProgressBarWindow(Window owner, ProgressBarViewModel dataContext, string windowTitle,
            string operationDescriptionText)
            : base(owner, dataContext)
        {
            Title = windowTitle;
            OperationDescriptionTextBlock.Text = operationDescriptionText;
        }

        protected override void Initialize() => InitializeComponent();
    }
}
