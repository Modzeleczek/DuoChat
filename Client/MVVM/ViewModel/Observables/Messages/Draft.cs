using Shared.MVVM.Core;
using System.Collections.ObjectModel;

namespace Client.MVVM.ViewModel.Observables.Messages
{
    public class Draft : ObservableObject
    {
        private string _content = string.Empty;
        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> AttachmentPaths { get; } =
            new ObservableCollection<string>();
    }
}
