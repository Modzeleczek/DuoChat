using Client.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows
{
    public partial class SettingsWindow : DialogWindow
    {
        public SettingsWindow(Window owner, SettingsViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
