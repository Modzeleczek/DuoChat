using Client.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.View.Windows.Conversations
{
    public partial class AddParticipantWindow : DialogWindow
    {
        public AddParticipantWindow(Window owner, AddParticipantViewModel dataContext)
            : base(owner, dataContext)
        {
            InitializeComponent();
        }
    }
}
