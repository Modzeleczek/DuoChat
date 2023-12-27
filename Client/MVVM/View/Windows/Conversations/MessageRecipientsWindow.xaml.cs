using Client.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class MessageRecipientsWindow : DialogWindow
    {
        public MessageRecipientsWindow(Window owner, MessageRecipientsViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
