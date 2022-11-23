using System.Collections.ObjectModel;

namespace Client.MVVM.Model
{
    public class Account
    {
        public string Nickname { get; set; }
        public ObservableCollection<Friend> Friends { get; set; }
    }
}
