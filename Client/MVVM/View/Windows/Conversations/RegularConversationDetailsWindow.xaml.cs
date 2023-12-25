using Client.MVVM.ViewModel.Conversations;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class RegularConversationDetailsWindow : DialogWindow
    {
        public RegularConversationDetailsWindow(Window owner,
            RegularConversationDetailsViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
