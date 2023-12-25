using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class EditConversationUIRequest : UIRequest
    {
        #region Properties
        public ulong Id { get; }
        public string Name { get; }
        #endregion

        public EditConversationUIRequest(ulong id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
