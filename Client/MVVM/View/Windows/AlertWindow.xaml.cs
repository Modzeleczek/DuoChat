using Client.MVVM.ViewModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.MVVM.View.Windows
{
    public partial class AlertWindow : DialogWindow
    {
        protected override void Initialize() => InitializeComponent();

        public AlertWindow(Window owner, DialogViewModel dataContext)
            : base(owner, dataContext) { }

        public static void GoodDialog(Window owner, string text = "")
        {
            new AlertWindow(owner, new AlertViewModel(
                text, Color.FromArgb(255, 0x00, 0xFF, 0x00))).ShowDialog();
        }

        public static void BadDialog(Window owner, string text = "")
        {
            new AlertWindow(owner, new AlertViewModel(
                text, Color.FromArgb(255, 0xFF, 0x00, 0x00))).ShowDialog();
        }

        public static void NeutralDialog(Window owner, string text = "")
        {
            new AlertWindow(owner, new AlertViewModel(
                text, Color.FromArgb(255, 0xFF, 0xFF, 0xFF))).ShowDialog();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
