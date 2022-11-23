using System.Collections.ObjectModel;

namespace Client.MVVM.Model
{
    public class LoggedUser : LocalUser
    {
        public ObservableCollection<Server> Servers { get; set; } // array list

        public LoggedUser(LocalUser user) :
            base(user.Name, user.Salt, user.Digest)
        {

        }
    }
}
