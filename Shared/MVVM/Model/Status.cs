namespace Shared.MVVM.Model
{
    public class Status
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; } // ewentualne dodatkowe dane

        public Status(int code = 0, string message = null, object data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }
    }
}
