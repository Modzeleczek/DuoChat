using Shared.MVVM.Core;
using System;
using System.Collections.ObjectModel;

namespace Client.MVVM.ViewModel.Observables
{
    public class Conversation : ObservableObject
    {
        public Participant Owner { get; }

        private string name = string.Empty;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Participant> Participants { get; } =
            new ObservableCollection<Participant>();

        public ObservableCollection<Message> Messages { get; } =
            new ObservableCollection<Message>();

        public Conversation(Participant owner)
        {
            Owner = owner;
        }

        public static Conversation Random(Random rng)
        {
            var ret = new Conversation(Participant.Random(rng))
            {
                Name = rng.Next().ToString()
            };
            int parCnt = rng.Next(0, 10);
            for (var i = 0; i < parCnt; i++)
                ret.Participants.Add(Participant.Random(rng));
            int msgCnt = rng.Next(0, 10);
            for (var i = 0; i < msgCnt; i++)
                ret.Messages.Add(Message.Random(rng));
            return ret;
        }
    }
}
