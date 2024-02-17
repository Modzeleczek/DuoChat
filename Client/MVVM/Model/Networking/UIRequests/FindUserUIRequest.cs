using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class FindUserUIRequest : UIRequest
    {
        #region Properties
        public string LoginFragment { get; }
        #endregion

        public FindUserUIRequest(string loginFragment)
        {
            LoginFragment = loginFragment;
        }
    }
}
