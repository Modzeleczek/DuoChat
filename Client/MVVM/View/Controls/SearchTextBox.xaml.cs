using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.View.Controls
{
    public partial class SearchTextBox : UserControl
    {
        #region Properties
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        #endregion

        #region Fields
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string),
                typeof(SearchTextBox), new PropertyMetadata(string.Empty));
        #endregion

        public SearchTextBox()
        {
            InitializeComponent();
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var expression = (sender as TextBox).GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource();
        }
    }
}
