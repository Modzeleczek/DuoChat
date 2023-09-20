using Shared.MVVM.Model.SQLiteStorage;
using Server.MVVM.Model.Persistence.Repositories;

namespace Server.MVVM.Model.Persistence
{
    public class Database : SQLiteDatabase
    {
        #region Properties
        /* Jak DbSety z Entity Frameworka, ale nie do końca,
        bo nie każda tabela z bazy danych ma swoje repozytorium. */
        public UserRepository Users { get; }
        #endregion

        public Database(string path) : base(path)
        {
            Users = new UserRepository(CreateConnection);
        }

        protected override string DDLEmbeddedResource()
        {
            return "Server.MVVM.Model.Persistence.database.sql";
        }
    }
}
