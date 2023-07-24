using Shared.MVVM.Core;

namespace Server.MVVM.ViewModel.Observables
{
    public abstract class Client : ObservableObject
    {
        public abstract string DisplayedName { get; set; }

        public Model.Client Model { get; }

        protected Client(Model.Client model)
        {
            Model = model;
        }
    }
}
