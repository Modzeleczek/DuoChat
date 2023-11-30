using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class AccountDto : IDto<(ulong id, string login)>
    {
        #region Properties
        public ulong Id { get; set; } = 0;

        /* Login jest identyfikatorem z punktu widzenia aplikacji i unikalną
        kolumną, a jednocześnie kluczem kandydującym (jednoznacznie
        identyfikuje każdy rekord tabeli) tabeli Account. Natomiast, z
        perspektywy bazy danych, Liczbowa kolumna Id jest faktycznym kluczem
        głównym tabeli Account, co ma na celu szybsze wykonywanie operacji
        łączenia tabel, bo szybciej porównuje się 2 liczby o stałej długości
        niż 2 stringi o dowolnych długościach. */
        public string Login { get; set; } = null!;

        public PublicKey PublicKey { get; set; } = null!;

        // 0 lub 1
        public byte IsBlocked { get; set; } = 0;
        #endregion

        public (ulong id, string login) GetRepositoryKey()
        {
            return (Id, Login);
        }
    }
}
