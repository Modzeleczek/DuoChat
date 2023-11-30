namespace Shared.MVVM.ViewModel.Results
{
    public class Success : Result
    {
        public object? Data { get; }

        public Success(object? data = null)
        {
            Data = data;
        }
    }
}
