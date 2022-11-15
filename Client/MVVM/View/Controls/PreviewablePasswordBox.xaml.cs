using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.View.Controls
{
    public partial class PreviewablePasswordBox : UserControl
    {
        private bool IsPasswordVisible = false;

        public PreviewablePasswordBox()
        {
            InitializeComponent();
        }

        public string Password
        {
            get
            {
                var pass = PasswordBox_.Password;
                SetValue(PasswordProperty, pass);
                // return (string)GetValue(PasswordProperty);
                return pass; // równoważne
            }
            set
            {
                PasswordBox_.Password = value;
                SetValue(PasswordProperty, value);
            }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string),
                typeof(PreviewablePasswordBox), new PropertyMetadata(string.Empty));

        private void PreviewPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!IsPasswordVisible)
            {
                IsPasswordVisible = true;
                PasswordBox_.Visibility = Visibility.Hidden;
                TextBox_.Text = Password;
                TextBox_.Visibility = Visibility.Visible;
            }
            else
            {
                IsPasswordVisible = false;
                PasswordBox_.Visibility = Visibility.Visible;
                TextBox_.Visibility = Visibility.Hidden;
            }
        }
    }
}
