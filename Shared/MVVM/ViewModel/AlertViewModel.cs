namespace Shared.MVVM.ViewModel
{
    public class AlertViewModel : WindowViewModel
    {
        public string Title { get; }
        public string Description { get; }
        public string ButtonText { get; }

        protected AlertViewModel(string title, string description, string buttonText)
        {
            Title = title;
            Description = description;
            ButtonText = buttonText;
        }
    }
}
