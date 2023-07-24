using System.Net;

namespace Server.MVVM.ViewModel.Observables
{
    public class Anonymous : Client
    {
        public override string DisplayedName
        {
            get
            {
                IPEndPoint ep = Model.RemoteEndPoint;
                return $"{ep.Address}:{ep.Port}";
            }
            set { OnPropertyChanged(); }
        }

        public Anonymous(Model.Client client) : base(client) {}
    }
}
