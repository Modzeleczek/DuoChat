using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.MVVM.View.Windows
{
    public partial class FormWindow : DialogWindow
    {
        private static readonly Translator d = Translator.Instance;
        private List<Control> _fields = new List<Control>();

        public FormWindow(Window owner, FormViewModel dataContext) : base(owner, dataContext)
        {
            ConfirmButton.CommandParameter = _fields;
            // cancel też ma dostawać pola, aby zdisposować potencjalne hasła
            CancelButton.CommandParameter = _fields;
        }

        protected override void Initialize() => InitializeComponent();

        public void AddTextField(string label, string initialValue = "")
        {
            var children = FieldsStackPanel.Children;
            children.Add(new Label { Content = d[label] });
            var textBox = new TextBox { Text = initialValue };
            children.Add(textBox);
            _fields.Add(textBox);
        }

        public void AddPasswordField(string label, string initialValue = "")
        {
            var children = FieldsStackPanel.Children;
            children.Add(new Label { Content = d[label] });
            var passwordBox = new PasswordBox { Password = initialValue };
            children.Add(passwordBox);
            _fields.Add(passwordBox);
        }

        public void AddHoverableTextField(string label, string[] buttonCommandPaths,
            string[] buttonTexts, string initialValue = "")
        {
            if (buttonCommandPaths.Length != buttonTexts.Length)
                throw new ArgumentException(
                    $"{nameof(buttonCommandPaths)} must be the same length as {nameof(buttonTexts)}");

            FieldsStackPanel.Children.Add(new Label { Content = d[label] });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            FieldsStackPanel.Children.Add(grid);

            var textBox = new TextBox { Text = initialValue };
            grid.Children.Add(textBox);
            Grid.SetColumn(textBox, 0);
            _fields.Add(textBox);

            var hoverPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal,
                Style = (Style)FindResource("HoverPanelStyle")
            };
            grid.Children.Add(hoverPanel);
            Grid.SetColumn(hoverPanel, 0);

            var buttonCount = buttonCommandPaths.Length;
            for (int i = 0; i < buttonCount; ++i)
            {
                var button = new Button
                {
                    Margin = new Thickness(7, 7, (i == buttonCount - 1) ? 20 : 7, 7),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Style = (Style)FindResource("CornerRadiusButtonStyle")
                };
                hoverPanel.Children.Add(button);

                button.SetBinding(Button.CommandProperty, new Binding
                {
                    Path = new PropertyPath(path: buttonCommandPaths[i]),
                    Mode = BindingMode.OneWay
                });
                /* handlerom dodawanych przycisków też przekazujemy _fields,
                aby viewmodel mógł bez bindowania tekstów edytować pola formularza */
                button.CommandParameter = _fields;

                /* nie bindujemy tekstu do przetłumaczenia, ale
                dostajemy już przetłumaczony w parametrze buttonTexts[i]
                var translator = Translator.Instance;
                button.SetBinding(Button.ContentProperty, new Binding
                {
                    Source = translator,
                    Path = new PropertyPath($"{nameof(translator.D)}.{buttonTexts[i]}"),
                    Mode = BindingMode.OneWay
                }); */
                button.Content = d[buttonTexts[i]];
            }
        }

        public void RemoveField(int index)
        {
            // nie sprawdzamy if (!(index >= 0 && index < _fields.Count)), aby lista wyrzuciła wyjątek
            _fields.RemoveAt(index);
            // na każde pole przypada Label i kontrolka
            FieldsStackPanel.Children.RemoveAt(2 * index);
            FieldsStackPanel.Children.RemoveAt(2 * index + 1);
        }
    }
}
