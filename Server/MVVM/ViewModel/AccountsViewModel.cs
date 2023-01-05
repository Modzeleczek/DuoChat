using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server.MVVM.ViewModel
{
    public class AccountsViewModel : ViewModel
    {
        private Model.Server _server;

        public AccountsViewModel(Window owner, Model.Server server)
        {
            window = owner;
            _server = server;
        }
    }
}
