using Client.MVVM.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace Client.MVVM.View.Windows
{
    public class DialogWindow : Window
    {
        private Storyboard closeStoryboard;
        private bool closeStoryboardCompleted = false;

        public DialogWindow() { }

        protected DialogWindow(Window owner, DialogViewModel dataContext)
        {
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
                Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
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
                Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
                closeStoryboard = new Storyboard();
                Storyboard.SetTarget(closeStoryboard, this);
                closeStoryboard.Children.Add(anim);
                closeStoryboard.Completed += closeStoryboard_Completed;
                Closing += Window_Closing;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!closeStoryboardCompleted)
            {
                closeStoryboard.Begin();
                e.Cancel = true;
            }
        }

        private void closeStoryboard_Completed(object sender, EventArgs e)
        {
            closeStoryboardCompleted = true;
            Close();
        }
    }
}
