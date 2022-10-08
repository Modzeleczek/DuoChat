using System.Collections.ObjectModel;
using System.Linq;

namespace Client.MVVM.Model
{
    public class Friend
    {   
        public string Nickname { get; set; }
        public string ImageSource { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        public string LastMessage => Messages.Last().Content_;
    }
}
