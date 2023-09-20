using Client.MVVM.Model.SQLiteStorage.Repositories;
using Shared.MVVM.Model.SQLiteStorage;

namespace Client.MVVM.Model.SQLiteStorage
{
    public class ServerDatabase : SQLiteDatabase
    {
        #region Properties
        public AccountRepository Accounts { get; }
        #endregion

        public ServerDatabase(string path) : base(path)
        {
            Accounts = new AccountRepository(CreateConnection);
        }

        protected override string DDLEmbeddedResource()
        {
            return "Client.MVVM.Model.SQLiteStorage.database.sql";
        }
    }
}
