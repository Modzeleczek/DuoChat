using Server.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Server.MVVM.View.Windows
{
    public partial class MainWindow : DialogWindow
    {
        public MainWindow(Window owner, MainViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();
    }
}
