namespace Client.MVVM.Model
{
    public class LoggedUser
    {
        public string LocalName { get; set; }
        public string LocalPassword { get; set; }
        public static LoggedUser Instance { get; set; } = new LoggedUser();

        private LoggedUser() { }
    }
}
