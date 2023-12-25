using Client.MVVM.ViewModel.Conversations;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class AdminConversationDetailsWindow : DialogWindow
    {
        public AdminConversationDetailsWindow(Window owner,
            AdminConversationDetailsViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
