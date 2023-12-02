﻿using Shared.MVVM.Model.SQLiteStorage;
using System.Data.SQLite;

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

        public bool Exists(ulong id)
        {
            // AccountsById zwraca uwagę tylko na Id, a ignoruje Login.
            return Exists((id, string.Empty));
        }
    }
}