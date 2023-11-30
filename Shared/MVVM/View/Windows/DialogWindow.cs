using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace Shared.MVVM.View.Windows
{
    public class DialogWindow : Window
    {
        private Storyboard? closeStoryboard;
        private bool closeStoryboardCompleted = false;
        public bool Closable { get; set; } = true;

        public DialogWindow() { }

        protected DialogWindow(Window? owner, object dataContext)
        {
            // TODO: usunąć Initialize, bo i tak wykonuje się jego nienadpisana wersja.
            Initialize();
            DataContext = dataContext;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            AddFadeAnimations();
        }

        protected virtual void Initialize() { }

        private void AddFadeAnimations()
        {
            var dur = new Duration(TimeSpan.FromMilliseconds(100));
            { // otwieranie okna
                var anim = new DoubleAnimation(0, 1, dur);
                Storyboard.SetTargetProperty(anim, new PropertyPath(path: "Opacity"));
                var strBrd = new Storyboard();
                strBrd.Children.Add(anim);
                var begStrBrd = new BeginStoryboard();
                begStrBrd.Storyboard = strBrd;
                var evTrig = new EventTrigger(Window.LoadedEvent);
                evTrig.Actions.Add(begStrBrd);
                Triggers.Add(evTrig);
            }
            { // zamykanie okna
                var anim = new DoubleAnimation(1, 0, dur);
                Storyboard.SetTargetProperty(anim, new PropertyPath(path: "Opacity"));
                closeStoryboard = new Storyboard();
                Storyboard.SetTarget(closeStoryboard, this);
                closeStoryboard.Children.Add(anim);
                closeStoryboard.Completed += closeStoryboard_Completed;
                Closing += Window_Closing;
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (Closable) // okno może być zamknięte
            {
                // jeszcze nie wykonaliśmy animacji zamknięcia okna
                if (!closeStoryboardCompleted)
                {
                    // rozpoczynamy animację zamknięcia okna
                    closeStoryboard!.Begin();
                    // zapobiegamy zamknięciu okna przed zakończeniem animacji
                    e.Cancel = true;
                }
                // false to domyślna wartość, ale jawnie ją ustawiamy
                else e.Cancel = false;
            }
            // okno nie może być zamknięte, więc zapobiegamy jego zamknięciu
            else e.Cancel = true;
        }

        private void closeStoryboard_Completed(object? sender, EventArgs e)
        {
            closeStoryboardCompleted = true;
            Close(); // wywołanie Close powoduje wykonanie handlera Window_Closing eventu Closing
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }
    }
}
