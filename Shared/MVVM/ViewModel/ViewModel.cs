using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using System;
using System.Windows;

namespace Shared.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        protected Window window;
        protected readonly Translator d = Translator.Instance;

        protected void UIInvoke(Action action) => Application.Current.Dispatcher.Invoke(action);
    }
}
