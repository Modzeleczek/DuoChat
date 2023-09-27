using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.MVVM.ViewModel
{
    public class AccountsViewModel : UserControlViewModel
    {
        public AccountsViewModel(DialogWindow owner, Model.Server server)
            : base(owner)
        {
            
        }
    }
}
