using Client.MVVM.Core;
using System.Collections.ObjectModel;

namespace Client.MVVM.Model
{
    public class Account : ObservableObject
    {
        public string login;
        public string Login
        {
            get => login;
            set { login = value; OnPropertyChanged(); }
        }

        public string password;
        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); }
        }

        public string nickname;
        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Friend> friends;
        public ObservableCollection<Friend> Friends
        {
            get => friends;
            private set { friends = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Conversation> conversations;
        public ObservableCollection<Conversation> Conversations
        {
            get => conversations;
            set { conversations = value; OnPropertyChanged(); }
        }
    }
}
