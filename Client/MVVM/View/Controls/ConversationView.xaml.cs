using Client.MVVM.ViewModel;
using System.Windows.Controls;

namespace Client.MVVM.View.Controls
{
    public partial class ConversationView : UserControl
    {
        #region Fields
        private bool _autoScroll;
        #endregion

        public ConversationView()
        {
            InitializeComponent();
        }

        private void MessageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // https://stackoverflow.com/a/19315242
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (MessageScrollViewer.VerticalOffset == MessageScrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    _autoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    _autoScroll = false;

                    if (MessageScrollViewer.VerticalOffset == 0)
                    {
                        // Użytkownik ręcznie doscrollował na samą górę.
                        var getMoreMessagesCommand = ((ConversationViewModel)DataContext).GetMoreMessages;
                        if (getMoreMessagesCommand.CanExecute(null))
                            getMoreMessagesCommand.Execute(null);
                    }
                }
            }

            // Content scroll event : auto-scroll eventually
            if (_autoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                // MessageScrollViewer.ScrollToBottom(); - potencjalna (nie sprawdzona) alternatywa
                MessageScrollViewer.ScrollToVerticalOffset(MessageScrollViewer.ExtentHeight);
            }
        }
    }
}
