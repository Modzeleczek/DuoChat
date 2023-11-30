using System.Data.SQLite;

namespace Shared.MVVM.Model.SQLiteStorage
{
    public interface ISQLiteConnector
    {
        SQLiteConnection CreateConnection();
    }
}
