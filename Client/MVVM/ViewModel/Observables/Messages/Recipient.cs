using Shared.MVVM.Core;
using System;

namespace Client.MVVM.ViewModel.Observables.Messages
{
    public class Recipient : ObservableObject
    {
        #region Properties
        public User Participant { get; }

        private bool _received = false;
        public bool Received
        {
            get => _received;
            private set { _received = value; OnPropertyChanged(); }
        }

        /* Binding w WPFie nie rozróżnia null od default przypisanego
        do nullowalnego DateTime, więc potrzeba property Received. */
        private DateTime? _receiveTime = null;
        public DateTime? ReceiveTime
        {
            get => _receiveTime;
            set
            {
                _receiveTime = value;
                OnPropertyChanged();

                Received = !(value is null);
            }
        }
        #endregion

        public Recipient(User participant)
        {
            Participant = participant;
        }
    }
}
