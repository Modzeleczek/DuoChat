using Client.MVVM.Core;
using System.Collections.ObjectModel;

namespace Client.MVVM.Model
{
    public class Server : ObservableObject
    {
        public string iPAddress;
        public string IPAddress
        {
            get => iPAddress;
            set { iPAddress = value; OnPropertyChanged(); }
        }

        public ushort port;
        public ushort Port
        {
            get => port;
            set { port = value; OnPropertyChanged(); }
        }

        public string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Account> accounts;
        public ObservableCollection<Account> Account
        {
            get => accounts;
            set { accounts = value; OnPropertyChanged(); }
        }
    }
}
