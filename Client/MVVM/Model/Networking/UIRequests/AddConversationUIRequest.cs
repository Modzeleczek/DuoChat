using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class AddConversationUIRequest : UIRequest
    {
        #region Properties
        public string Name { get; }
        #endregion
        
        public AddConversationUIRequest(string name)
        {
            Name = name;
        }
    }
}
