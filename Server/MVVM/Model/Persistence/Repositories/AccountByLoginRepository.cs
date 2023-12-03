using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    // Login jest kluczem.
    public class AccountByLoginRepository : AccountRepository
    {
        public AccountByLoginRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl,
            (ulong id, string login) key)
        {
            parColl.AddWithValue($"@{F_login}", key.login);
        }

        protected override string KeyCondition()
        {
            return $"{F_login} = @{F_login}";
        }

        protected override bool KeysEqual((ulong id, string login) key1, (ulong id, string login) key2)
        {
            return key1.login.Equals(key2.login);
        }

        public bool Exists(string login)
        {
            return Exists((0, login));
        }

        public AccountDto Get(string login)
        {
            return Get((0, login));
        }

        public void Update(string login, AccountDto dto)
        {
            Update((0, login), dto);
        }
    }
}
