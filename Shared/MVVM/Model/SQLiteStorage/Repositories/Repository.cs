using Shared.MVVM.Core;
using System;
using System.Data.SQLite;

namespace Shared.MVVM.Model.SQLiteStorage.Repositories
{
    public abstract class Repository
    {
        private Func<SQLiteConnection> _connectionCreator;

        public Repository(Func<SQLiteConnection> connectionCreator)
        {
            _connectionCreator = connectionCreator;
        }

        protected SQLiteConnection CreateConnection() => _connectionCreator();

        #region Errors
        protected Error QueryError(Exception inner = null) =>
            new Error(inner, "|Error occured while| |executing query.|");

        protected Error ConnectionError() =>
            new Error("|Error occured while| " +
                "|connecting to server's database file.|");

        protected Error NotExactly1RowError() =>
            new Error("|Number of rows affected by the query is other than 1.|");
        #endregion
    }
}
