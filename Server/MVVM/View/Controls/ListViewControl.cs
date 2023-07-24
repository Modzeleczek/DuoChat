using System.Windows.Controls;

namespace Server.MVVM.View.Controls
{
    public class ListViewControl : UserControl
    {
        protected void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = (ListView)sender;
            if (listView.SelectedItem == null)
                listView.UnselectAll();
        }
    }
}
