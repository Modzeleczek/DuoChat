using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server.MVVM.ViewModel
{
    public class ConnectedClientsViewModel : ViewModel
    {
        public ConnectedClientsViewModel(Window owner, Model.Server server)
        {
            window = owner;
        }
    }
}
