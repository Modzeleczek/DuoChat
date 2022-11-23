using Client.MVVM.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.View.Windows
{
    public partial class FormWindow : DialogWindow
    {
        public class Field
        {
            public string Name { get; set; }
            public string InitialValue { get; set; }
            // public string BindingPath { get; set; }
            public bool IsPassword { get; set; } // do haseł

            public Field(string name = "", string initialValue = "", bool isPassword = false)
            {
                Name = name;
                InitialValue = initialValue;
                IsPassword = isPassword;
            }
        }

        protected override void Initialize() => InitializeComponent();

        public FormWindow(Window owner, FormViewModel dataContext, string windowTitle,
            Field[] fields, string confirmationButtonText = "Ok")
            : base(owner, dataContext)
        {

            var inpCtrls = new Control[fields.Length];
            var chn = FieldsStackPanel.Children;
            for (int i = 0; i < fields.Length; ++i)
            {
                var f = fields[i];
                var lbl = new Label { Content = f.Name };
                /* w xaml.cs (code behind) zamiast bindować tłumacza, można bezpośrednio
                ustawić lbl.Content
                lbl.SetBinding(Label.ContentProperty, new Binding
                {
                    Converter = Strings.Instance,
                    ConverterParameter = f.Name
                }); */
                chn.Add(lbl);
                Control ctrl;
                /* robimy bindingi jeżeli w viewmodelu chcemy mieć dostęp (pobierać i ewentualnie ustawiać) dane z formularza (FormWindow)
                var bnd = new Binding();
                // jako bnd.Source jak zwykle ma być DataContext, czyli viewmodel ustawiony na górze konstruktora FormWindow
                bnd.Path = new PropertyPath(f.BindingPath);
                bnd.Mode = BindingMode.TwoWay;
                if (f.IsPassword)
                {
                    var ppb = ctrl = new PreviewablePasswordBox { Password = f.InitialValue };
                    // dependecy property stworzone we własnej kontrolce może być targetem bindingu
                    ppb.SetBinding(PreviewablePasswordBox.PasswordProperty, bnd);
                }
                else
                {
                    var tb = ctrl = new TextBox { Text = f.InitialValue };
                    tb.SetBinding(TextBox.TextProperty, bnd);
                } */
                if (f.IsPassword) ctrl = new PasswordBox { Password = f.InitialValue };
                else ctrl = new TextBox { Text = f.InitialValue };
                chn.Add(ctrl);
                inpCtrls[i] = ctrl;
            }

            FormWindowName.Title = windowTitle;

            ConfirmationButton.Content = confirmationButtonText;
            /* if (confirmationButtonText != null)
                ConfirmationButton.SetBinding(Button.CommandProperty, new Binding
                { Path = new PropertyPath(confirmationButtonText) }); */
            ConfirmationButton.CommandParameter =
                CancellationButton.CommandParameter = inpCtrls;
        }
    }
}
