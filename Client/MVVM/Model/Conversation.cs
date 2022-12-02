using Client.MVVM.Core;
using System.Collections.Generic;

namespace Client.MVVM.Model
{
    public class Conversation : ObservableObject
    {
        public List<User> Participants { get; set; }

        public List<Message> Messages { get; set; }

        private string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }
    }
}
