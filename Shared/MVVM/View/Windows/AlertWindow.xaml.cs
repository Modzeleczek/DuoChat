using Shared.MVVM.ViewModel;
using System.Windows;

namespace Shared.MVVM.View.Windows
{
    public partial class AlertWindow : DialogWindow
    {
        public AlertWindow(Window? owner, AlertViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
