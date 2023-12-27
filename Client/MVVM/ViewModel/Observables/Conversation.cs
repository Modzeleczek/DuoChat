using Client.MVVM.ViewModel.Observables.Messages;
using Shared.MVVM.Core;
using System.Collections.ObjectModel;

namespace Client.MVVM.ViewModel.Observables
{
    public class Conversation : ObservableObject
    {
        public ulong Id { get; set; } = 0;

        public User Owner { get; set; } = null!;

        private string _name = null!;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public Draft Draft { get; } = new Draft();

        private uint _unreceivedMessagesCount = 0;
        public uint UnreceivedMessagesCount
        {
            get => _unreceivedMessagesCount;
            set { _unreceivedMessagesCount = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConversationParticipation> Participations { get; } =
            new ObservableCollection<ConversationParticipation>();

        public ObservableCollection<Message> Messages { get; } =
            new ObservableCollection<Message>();
    }
}
