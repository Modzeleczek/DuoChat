using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class SearchUsersUIRequest : UIRequest
    {
        #region Properties
        public string LoginFragment { get; }
        #endregion

        public SearchUsersUIRequest(string loginFragment)
        {
            LoginFragment = loginFragment;
        }
    }
}
