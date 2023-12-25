using Client.MVVM.ViewModel.Conversations;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class OwnerConversationDetailsWindow : DialogWindow
    {
        public OwnerConversationDetailsWindow(Window owner,
            OwnerConversationDetailsViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
