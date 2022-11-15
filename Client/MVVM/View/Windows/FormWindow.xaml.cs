using Client.MVVM.View.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.View.Windows
{
    public partial class FormWindow : Window
    {
        public class Field
        {
            public string Name { get; set; }
            public string InitialValue { get; set; }
            public bool IsPassword { get; set; } // do haseł

            public Field(string name = "", string initialValue = "", bool isPassword = false)
            {
                Name = name;
                InitialValue = initialValue;
                IsPassword = isPassword;
            }
        }

        public FormWindow(Window owner, object dataContext, string windowTitle,
            Field[] fields, string confirmationButtonText = "Ok")
        {
            InitializeComponent();
            DataContext = dataContext;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

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
                if (f.IsPassword)
                    // ctrl = new PasswordBox();
                    // TODO: pole do hasła jako UserControl (poziomy StackPanel z PasswordBoxem i przyciskiem (albo czymś innym co reaguje na hover) do odkrywania)
                    ctrl = new PreviewablePasswordBox { Password = f.InitialValue };
                else ctrl = new TextBox { Text = f.InitialValue };
                chn.Add(ctrl);
                inpCtrls[i] = ctrl;
            }

            FormWindowName.Title = windowTitle;

            ConfirmationButton.Content = confirmationButtonText;
            /* if (confirmationButtonText != null)
                ConfirmationButton.SetBinding(Button.CommandProperty, new Binding
                { Path = new PropertyPath(confirmationButtonText) }); */
            ConfirmationButton.CommandParameter = inpCtrls;
        }
    }
}
