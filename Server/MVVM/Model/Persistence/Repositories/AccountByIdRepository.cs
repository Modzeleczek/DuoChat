using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Server.MVVM.Model.Persistence.Repositories
{
    // Id jest kluczem.
    public class AccountByIdRepository : AccountRepository
    {
        public AccountByIdRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl,
            (ulong id, string login) key)
        {
            parColl.AddWithValue($"@{F_id}", key.id);
        }

        protected override string KeyCondition()
        {
            return $"{F_id} = @{F_id}";
        }

        protected override bool KeysEqual((ulong id, string login) key1, (ulong id, string login) key2)
        {
            return key1.id.Equals(key2.id);
        }

        public bool Exists(ulong id)
        {
            // AccountsById zwraca uwagę tylko na Id, a ignoruje Login.
            return Exists((id, string.Empty));
        }

        public IEnumerable<AccountDto> GetByIds(IEnumerable<ulong> ids)
        {
            if (!ids.Any())
                return Enumerable.Empty<AccountDto>();

            var query = $"SELECT * FROM {TABLE} WHERE {F_id} IN ({string.Join(',', ids)});";
            return ExecuteReader(query);
        }
    }
}
