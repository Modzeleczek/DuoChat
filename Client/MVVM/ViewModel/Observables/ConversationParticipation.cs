using Shared.MVVM.Core;
using System;

namespace Client.MVVM.ViewModel.Observables
{
    public class ConversationParticipation : ObservableObject
    {
        public ulong ConversationId { get; set; } = 0;
        public Conversation Conversation { get; set; } = null!;

        public ulong ParticipantId { get; set; } = 0;
        public User Participant { get; set; } = null!;

        public DateTime JoinTime { get; set; } = default;

        private bool _isAdministrator = false;
        public bool IsAdministrator
        {
            get => _isAdministrator;
            set { _isAdministrator = value; OnPropertyChanged(); }
        }
    }
}
