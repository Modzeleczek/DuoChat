using Client.MVVM.Model.Networking;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Microsoft.Win32;
using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class AttachmentSelectorViewModel : ConversationCancellableViewModel
    {
        #region Commands
        public RelayCommand AddAttachments { get; }
        public RelayCommand RemoveAttachment { get; }
        #endregion

        #region Properties
        public ObservableCollection<string> AttachmentPaths { get; }
        #endregion

        private AttachmentSelectorViewModel(ClientMonolith client, Conversation conversation)
            : base(client, conversation)
        {
            AttachmentPaths = conversation.Draft.AttachmentPaths;

            AddAttachments = new RelayCommand(_ =>
            {
                var d = Translator.Instance;
                var dialog = new OpenFileDialog()
                {
                    Title = d["|Add_attachments|"],
                    InitialDirectory = App.FileDialogInitialDirectory,
                    Filter = d["|All files|"] + " (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = true
                };
                if (dialog.ShowDialog() != true)
                    return;

                var paths = dialog.FileNames;
                foreach (var path in paths)
                {
                    if (!File.Exists(path))
                    {
                        Alert($"|File| {path} |does not exist.|");
                        continue;
                    }
                    AttachmentPaths.Add(path);
                }

                if (paths.Length > 0)
                    App.UpdateFileDialogInitialDirectory(paths[0]);
            });

            RemoveAttachment = new RelayCommand(obj =>
            {
                AttachmentPaths.Remove((string)obj!);
            });
        }

        public static Result ShowDialog(Window owner, ClientMonolith client, Conversation conversation)
        {
            var vm = new AttachmentSelectorViewModel(client, conversation);
            var win = new AttachmentSelectorWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
