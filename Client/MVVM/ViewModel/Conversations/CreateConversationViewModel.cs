using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.Conversations
{
    public class CreateConversationViewModel : FormViewModel
    {
        public CreateConversationViewModel()
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e!;
                window = win;
                win.AddTextField("|Name|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls!;

                var name = ((TextBox)fields[0]).Text;
                if (string.IsNullOrEmpty(name))
                {
                    Alert("|Conversation name cannot be empty|.");
                    return;
                }

                if (Encoding.UTF8.GetBytes(name).Length > 255)
                {
                    Alert("|UTF-8 encoded conversation name must be at most 255 bytes long|.");
                    return;
                }

                OnRequestClose(new Success(name));
            });
        }
    }
}
