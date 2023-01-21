using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using System;
using System.Windows;

namespace Shared.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        protected DialogWindow window;
        protected readonly Translator d = Translator.Instance;

        protected void UIInvoke(Action action) => Application.Current.Dispatcher.Invoke(action);
    }
}
