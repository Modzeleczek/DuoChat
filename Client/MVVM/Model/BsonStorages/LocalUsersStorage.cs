using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using System.Collections.Generic;

namespace Client.MVVM.Model.BsonStorages
{
    public class LocalUsersStorage
        : BsonStorage<LocalUser, LocalUserPrimaryKey, LocalUsersStorage.BsonStructure>
    {
        #region Classes
        public class BsonStructure
        {
            public bool IsLogged { get; set; } = false;
            public string LoggedUserName { get; set; } = string.Empty;
            public int ActiveLanguageId { get; set; } = 0;
            public int ActiveThemeId { get; set; } = 0;
            public List<LocalUser> Users { get; set; } = new List<LocalUser>();
        }
        #endregion

        public LocalUsersStorage(string bsonFilePath) : base(bsonFilePath) { }

        #region Errors
        protected override string LoadErrorMsg() =>
            "|Could not| |load| |local users' BSON file|.";
        protected override string SaveErrorMsg() =>
            "|Could not| |save| |local users' BSON file|.";

        protected override Error ItemAlreadyExistsError(LocalUserPrimaryKey key) =>
            new Error(AlreadyExistsMsg(key));

        public static string AlreadyExistsMsg(LocalUserPrimaryKey key) =>
            $"|Local user with name| '{key}' |already exists.|";

        protected override Error ItemNotExistsError(LocalUserPrimaryKey key) =>
            new Error(NotExistsMsg(key));

        public static string NotExistsMsg(LocalUserPrimaryKey key) =>
            $"|Local user with name| '{key}' |does not exist.|";
        #endregion

        protected override List<LocalUser> GetInternalList(BsonStructure structure)
        {
            return structure.Users;
        }

        protected override LocalUserPrimaryKey GetPrimaryKey(LocalUser item)
        {
            return item.GetPrimaryKey();
        }

        protected override bool KeysEqual(LocalUserPrimaryKey a, LocalUserPrimaryKey b)
        {
            return a.Equals(b);
        }

        public void SetLogged(bool isLogged, LocalUserPrimaryKey key = default)
        {
            var structure = Load();
            structure.IsLogged = isLogged;
            structure.LoggedUserName = key.Name;
            Save(structure);
        }

        public LocalUserPrimaryKey? GetLogged()
        {
            var structure = Load();
            if (!structure.IsLogged)
                return null;
            return new LocalUserPrimaryKey(structure.LoggedUserName);
        }

        public void SetActiveLanguage(int id)
        {
            var structure = Load();
            structure.ActiveLanguageId = id;
            Save(structure);
        }

        public int GetActiveLanguage()
        {
            return Load().ActiveLanguageId;
        }

        public void SetActiveTheme(int id)
        {
            var structure = Load();
            structure.ActiveThemeId = id;
            Save(structure);
        }

        public int GetActiveTheme()
        {
            return Load().ActiveThemeId;
        }
    }
}
