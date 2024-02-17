using Client.MVVM.ViewModel.Conversations;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class FindUserWindow : DialogWindow
    {
        public FindUserWindow(Window owner, FindUserViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
