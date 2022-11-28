using System.Collections.Generic;

namespace Client.MVVM.Model
{
    public class Conversation
    {
        public List<User> Participants { get; set; }
        public List<Message> Messages { get; set; }
    }
}
