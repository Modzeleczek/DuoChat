using Client.MVVM.ViewModel.Conversations;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class AttachmentSelectorWindow : DialogWindow
    {
        public AttachmentSelectorWindow(Window owner, AttachmentSelectorViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
