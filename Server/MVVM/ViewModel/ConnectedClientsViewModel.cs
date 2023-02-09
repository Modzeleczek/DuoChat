using Shared.MVVM.View.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.MVVM.ViewModel
{
    public class ConnectedClientsViewModel : ViewModel
    {
        public ConnectedClientsViewModel(DialogWindow owner, Model.Server server)
        {
            window = owner;
        }
    }
}
