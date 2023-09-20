using Shared.MVVM.Model.Cryptography;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class UserDTO
    {
        public long Id { get; set; }

        /* Login jest identyfikatorem z punktu widzenia aplikacji i unikalną
        kolumną, a jednocześnie kluczem kandydującym (jednoznacznie
        identyfikuje każdy rekord tabeli) tabeli User. Natomiast, z
        perspektywy bazy danych, Liczbowa kolumna Id jest faktycznym kluczem
        głównym tabeli User, co ma na celu szybsze wykonywanie operacji
        łączenia tabel, bo szybciej porównuje się 2 liczby o stałej długości
        niż 2 stringi o dowolnych długościach. */
        public string Login { get; set; }

        public PublicKey PublicKey { get; set; }
    }
}
