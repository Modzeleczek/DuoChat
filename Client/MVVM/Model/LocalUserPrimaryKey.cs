using Shared.MVVM.Core;

namespace Client.MVVM.Model
{
    public struct LocalUserPrimaryKey
    {
        public string Name { get; set; }

        public LocalUserPrimaryKey(string name)
        {
            Name = name;

            ValidateName();
        }

        // Wyrzuca Error z opisem błędu.
        private void ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new Error("|Username cannot be empty.|");
            /* Nie możemy pozwolić na stworzenie drugiego użytkownika o takiej samej nazwie
            case-insensitive, ponieważ NTFS ma case-insensitive nazwy plików i katalogów;
            najprościej temu zapobiec, wymuszając nazwy użytkowników
            złożone z tylko małych liter. */
            foreach (var c in Name)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                    throw new Error("|Username may contain only lowercase letters and digits.|");
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalUserPrimaryKey other && Name.Equals(other.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
