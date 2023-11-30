using Shared.MVVM.Model.SQLiteStorage;
using Server.MVVM.Model.Persistence.Repositories;

namespace Server.MVVM.Model.Persistence
{
    public class Database : SQLiteDatabase
    {
        #region Properties
        /* Jak DbSety z Entity Frameworka, ale nie do końca,
        bo nie każda tabela z bazy danych musi mieć repozytorium. */
        public AccountByIdRepository AccountsById { get; }
        public AccountByLoginRepository AccountsByLogin { get; }
        public ClientIPBlockRepository ClientIPBlocks { get; }
        public ConversationRepository Conversations { get; }
        #endregion

        public Database(string path) : base(path)
        {
            AccountsById = new AccountByIdRepository(this);
            AccountsByLogin = new AccountByLoginRepository(this);
            ClientIPBlocks = new ClientIPBlockRepository(this);
            Conversations = new ConversationRepository(this);
        }

        protected override string DDLEmbeddedResource()
        {
            return "Server.MVVM.Model.Persistence.database.sql";
        }
    }
}
