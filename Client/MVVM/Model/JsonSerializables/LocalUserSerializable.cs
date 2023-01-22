using System;

namespace Client.MVVM.Model.JsonSerializables
{
    public class LocalUserSerializable
    {
        #region Properties
        public string Name { get; set; } // unikalny identyfikator

        public byte[] PasswordSalt { get; set; }

        public byte[] PasswordDigest { get; set; }

        private byte[] _dbInitializationVector;
        public byte[] DbInitializationVector
        {
            get => _dbInitializationVector;
            set
            {
                // IV musi mieć długość równą długości bloku (dla Rijndaela zgodnego ze
                // specyfikacją AESa blok musi być 128-bitowy)
                if (value.Length != 128 / 8)
                    throw new ArgumentException("Database initialization vector is not 128 bits long.");
                _dbInitializationVector = value;
            }
        }

        private byte[] _dbSalt;
        public byte[] DbSalt
        {
            get => _dbSalt;
            set
            {
                // używamy 128-bitowych kluczy w AES
                if (value.Length != 128 / 8)
                    throw new ArgumentException("Database salt is not 128 bits long.");
                _dbSalt = value;
            }
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is LocalUserSerializable)) return false;
            var user = (LocalUserSerializable)obj;
            return KeyEquals(user.Name);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool KeyEquals(string name)
        {
            return Name == name;
        }

        public LocalUser ToObservable() =>
            new LocalUser
            {
                Name = Name,
                PasswordSalt = PasswordSalt,
                PasswordDigest = PasswordDigest,
                DbInitializationVector = DbInitializationVector,
                DbSalt = DbSalt
            };
    }
}
