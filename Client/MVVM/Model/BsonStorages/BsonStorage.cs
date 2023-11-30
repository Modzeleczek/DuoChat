using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using System;
using System.IO;
using Shared.MVVM.Core;
using System.Collections.Generic;

namespace Client.MVVM.Model.BsonStorages
{
    public abstract class BsonStorage<Serializable, PrimaryKey, BsonStructure>
        where BsonStructure : new() // Potrzebne do BSON-deserializacji.
    {
        #region Fields
        protected readonly string _bsonFilePath;
        #endregion

        protected BsonStorage(string bsonFilePath)
        {
            _bsonFilePath = bsonFilePath;

            CreateOrValidateFile();
        }

        private void CreateOrValidateFile()
        {
            if (File.Exists(_bsonFilePath))
            {
                // Plik istnieje, więc sprawdzamy jego strukturę.
                ValidateBsonStructureInFile();
                return;
            }

            // Plik jeszcze nie istnieje i musi zostać utworzony.
            try { Save(new BsonStructure()); }
            catch (Error e)
            {
                e.Prepend($"|Could not| |create| |file| '{_bsonFilePath}'.");
                throw;
            }
        }

        private void ValidateBsonStructureInFile()
        {
            /* Sprawdzamy, czy parser BSONa wczyta plik, co będzie
            znaczyło, że plik ma prawidłową strukturę. Natomiast nie
            sprawdzamy, czy dane w pliku mają sens, czyli np. czy
            użytkownicy z pliku mają swoje katalogi. */
            try { LoadWithoutCheckingExistence(); }
            catch (Error)
            {
                throw new Error($"|File| '{_bsonFilePath}' " +
                    "|does not contain| |valid BSON structure|.");
            }
        }

        protected BsonStructure Load()
        {
            if (!File.Exists(_bsonFilePath))
                throw new Error($"|File| '{_bsonFilePath}' |does not exist.|");

            return LoadWithoutCheckingExistence();
        }

        private BsonStructure LoadWithoutCheckingExistence()
        {
            try
            {
                using (var fs = File.OpenRead(_bsonFilePath))
                using (var br = new BinaryReader(fs))
                using (var bdr = new BsonDataReader(br, false, DateTimeKind.Utc))
                {
                    var ser = new JsonSerializer();
                    var structure = ser.Deserialize<BsonStructure>(bdr);
                    return structure!;
                }
            }
            catch (Exception e)
            {
                // Plik ma nieprawidłową strukturę.
                throw new Error(e, LoadErrorMsg(), "|Error occured while| " +
                    $"|deserializing| |from| |file| '{_bsonFilePath}'.");
            }
        }

        protected void Save(BsonStructure data)
        {
            // jeżeli plik nie istnieje, to zostanie stworzony
            try
            {
                using (var fs = File.OpenWrite(_bsonFilePath))
                using (var bw = new BinaryWriter(fs))
                using (var bdw = new BsonDataWriter(bw))
                {
                    var ser = new JsonSerializer();
                    ser.Serialize(bdw, data);
                    return;
                }
            }
            catch (Exception e)
            {
                throw new Error(e, SaveErrorMsg(), "|Error occured while| " +
                    $"|serializing| |to| |file| '{_bsonFilePath}'.");
            }
        }

        #region Errors
        protected abstract string LoadErrorMsg();
        protected abstract string SaveErrorMsg();
        protected abstract Error ItemAlreadyExistsError(PrimaryKey key);
        protected abstract Error ItemNotExistsError(PrimaryKey key);
        #endregion

        // Wzorzec Template Method
        public void Add(Serializable item)
        {
            var structure = Load();

            var items = GetInternalList(structure);
            var key = GetPrimaryKey(item);
            if (Exists(items, key))
                throw ItemAlreadyExistsError(key);

            items.Add(item);
            Save(structure);
        }

        protected abstract List<Serializable> GetInternalList(BsonStructure structure);

        protected abstract PrimaryKey GetPrimaryKey(Serializable item);

        public List<Serializable> GetAll()
        {
            return GetInternalList(Load());
        }

        private int IndexOf(List<Serializable> items, PrimaryKey key)
        {
            for (int i = 0; i < items.Count; ++i)
                if (KeysEqual(GetPrimaryKey(items[i]), key))
                    return i;
            return -1;
        }

        protected abstract bool KeysEqual(PrimaryKey a, PrimaryKey b);

        private bool Exists(List<Serializable> items, PrimaryKey key)
        {
            return IndexOf(items, key) != -1;
        }

        public bool Exists(PrimaryKey key)
        {
            return Exists(GetInternalList(Load()), key);
        }

        public Serializable Get(PrimaryKey key)
        {
            var items = GetInternalList(Load());

            var index = IndexOf(items, key);
            if (index != -1)
                return items[index];
            throw ItemNotExistsError(key);
        }

        public void Update(PrimaryKey key, Serializable item)
        {
            var structure = Load();
            var items = GetInternalList(structure);

            int oldItemIndex = IndexOf(items, key);
            if (oldItemIndex == -1)
                throw ItemNotExistsError(key);

            var newKey = GetPrimaryKey(item);
            int newItemIndex = IndexOf(items, newKey);
            /* W obiekcie item można przekazać nową wartość
            klucza głównego, ale nie może być zajęta.
            Jeżeli klucz główny obiektu item istnieje w liście */
            if (newItemIndex != -1)
            {
                /* newUserIndex == oldUserIndex jest równoważne KeysEqual(newKey, key).
                Jeżeli klucze główne nie są równe, czyli próbujemy usunąć z listy obiekt
                o kluczu głównym key i dodać nowy obiekt item o kluczu głównym, który już
                jest zajęty. */
                if (newItemIndex != oldItemIndex)
                    throw ItemAlreadyExistsError(newKey);

                /* Jeżeli klucz główny obiektu item jest równy key -
                metodą Update nie zmieniamy klucza głównego. */
                items[oldItemIndex] = item;
                Save(structure);
            }
            // Jeżeli klucz główny obiektu item nie istnieje w liście
            else
            {
                /* Metodą Update zmieniamy klucz główny obiektu o
                aktualnym kluczu głównym key. */
                items[oldItemIndex] = item;
                Save(structure);
            }

            /* Do następnego commita
            if (newItemIndex != -1 && newItemIndex != oldItemIndex)
                throw ItemAlreadyExistsError(newKey);

            items[oldItemIndex] = item;
            Save(structure); */
        }

        public void Delete(PrimaryKey key)
        {
            var structure = Load();
            var items = GetInternalList(structure);

            var index = IndexOf(items, key);
            if (index == -1)
                return;

            items.RemoveAt(index);
            Save(structure);
            /* Nie wyrzucamy wyjątku, jeżeli nie istnieje
            obiekt o kluczu, który chcemy usunąć - zakładamy,
            że operacja usuwania została wykonana. */
        }

        public void DeleteMany(Predicate<Serializable> predicate)
        {
            var structure = Load();
            var items = GetInternalList(structure);

            items.RemoveAll(predicate);
            Save(structure);
        }
    }
}
