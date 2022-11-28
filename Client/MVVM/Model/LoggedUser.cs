using System.Collections.ObjectModel;

namespace Client.MVVM.Model
{
    public class LoggedUser : LocalUser
    {
        private ObservableCollection<Server> servers;
        public ObservableCollection<Server> Servers
        {
            get => servers;
            private set { servers = value; OnPropertyChanged(); }
        }

        public LoggedUser(LocalUser user) :
            base(user.Name, user.PasswordSalt, user.PasswordDigest,
                user.DBInitializationVector, user.DBSalt)
        { }
    }
}
