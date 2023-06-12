using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public abstract class RsaKey
    {
        public abstract void ImportTo(RSA rsa);
    }
}
