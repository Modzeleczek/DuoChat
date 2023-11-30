using Shared.MVVM.Core;
using System.Collections.Generic;

namespace Client.MVVM.Model.BsonStorages
{
    public class ServersStorage : BsonStorage<ViewModel.Observables.Server, ServerPrimaryKey,
        ServersStorage.BsonStructure>
    {
        #region Classes
        public class BsonStructure
        {
            public List<ViewModel.Observables.Server> Servers { get; set; } =
                new List<ViewModel.Observables.Server>();
        }
        #endregion

        public ServersStorage(string bsonFilePath) : base(bsonFilePath) { }

        #region Errors
        protected override string LoadErrorMsg() =>
            "|Could not| |load| |local user's server BSON file|.";

        protected override string SaveErrorMsg() =>
            "|Could not| |save| |local user's server BSON file|.";

        protected override Error ItemAlreadyExistsError(ServerPrimaryKey key) =>
            new Error(AlreadyExistsMsg(key));

        public static string AlreadyExistsMsg(ServerPrimaryKey key) =>
            $"|Server with IP address| {key.IpAddress} " +
            $"|and port| {key.Port} |already exists.|";

        protected override Error ItemNotExistsError(ServerPrimaryKey key) =>
            new Error(NotExistsMsg(key));

        public static string NotExistsMsg(ServerPrimaryKey key) =>
            $"|Server with IP address| {key.IpAddress} " +
            $"|and port| {key.Port} |does not exist.|";
        #endregion

        protected override List<ViewModel.Observables.Server> GetInternalList(BsonStructure structure)
        {
            return structure.Servers;
        }

        protected override ServerPrimaryKey GetPrimaryKey(ViewModel.Observables.Server item)
        {
            return item.GetPrimaryKey();
        }

        protected override bool KeysEqual(ServerPrimaryKey a, ServerPrimaryKey b)
        {
            return a.Equals(b);
        }
    }
}
