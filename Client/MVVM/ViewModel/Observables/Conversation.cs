using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Client.MVVM.ViewModel.Observables
{
    public class Conversation : ObservableObject
    {
        public Participant Owner { get; set; }

        private string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public List<Participant> Participants { get; set; }

        public ObservableCollection<Message> Messages { get; set; }

        public static Conversation Random(Random rng)
        {
            var ret = new Conversation
            {
                Owner = Participant.Random(rng),
                Name = rng.Next().ToString()
            };
            int parCnt = rng.Next(0, 10);
            ret.Participants = new List<Participant>(parCnt);
            for (var i = 0; i < parCnt; i++)
                ret.Participants.Add(Participant.Random(rng));
            int msgCnt = rng.Next(0, 10);
            ret.Messages = new ObservableCollection<Message>();
            for (var i = 0; i < msgCnt; i++)
                ret.Messages.Add(Message.Random(rng));
            return ret;
        }
    }
}
