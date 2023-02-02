using Client.MVVM.Model.JsonSerializables;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Client.MVVM.Model.BsonStorages
{
    public class ServersStorage : BsonStorage
    {
        public const string SERVERS_BSON = "servers.bson";

        private LocalUser user;

        private string serversBsonPath => Path.Combine(user.DirectoryPath, SERVERS_BSON);

        public ServersStorage(LocalUser user)
        {
            this.user = user;
        }

        private class BsonStructure
        {
            public List<ServerSerializable> Servers { get; set; } = new List<ServerSerializable>();
        }

        private Status ServerAlreadyExistsStatus(int code, IPv4Address ipAddress, Port port) =>
            new Status(code, null, d["Server with IP address"], ipAddress,
                d["and port"], port, d["already exists."]);

        private Status ServerDoesNotExistStatus(int code, IPv4Address ipAddress, Port port) =>
            new Status(code, null, d["Server with IP address"], ipAddress,
                d["and port"], port, d["does not exist."]);

        private Status ServerFileAlreadyExistsStatus(int code, IPv4Address ipAddress, Port port) =>
            new Status(code, null, d["Server's file with name"], ServerFileName(ipAddress, port),
                d["already exists."]);
        
        private Status ServerFileDoesNotExistStatus(int code, IPv4Address ipAddress, Port port) =>
            new Status(code, null, d["Server's file with name"], ServerFileName(ipAddress, port),
                d["does not exist."]);

        public Status EnsureValidDatabaseState()
        {
            var path = user.DirectoryPath;
            if (!Directory.Exists(path))
            {
                try
                { Directory.CreateDirectory(path); }
                catch (Exception)
                {
                    return new Status(-1, null, d["Error occured while"], d["creating"],
                        d["directory"], $"{path}."); // -1
                }
            }
            path = serversBsonPath;
            if (!File.Exists(path))
            {
                var saveStatus = Save(new BsonStructure());
                if (saveStatus.Code != 0)
                    return saveStatus.Prepend(-2, d["Error occured while"], d["creating"],
                        d["file"], $"{path}."); // -2
            }

            var loadStatus = Load<BsonStructure>(path);
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-3); // -3

            var structure = (BsonStructure)loadStatus.Data;
            /* usuwamy serwery obecne w BSONie, ale
            nieposiadające swoich plików */
            var servers = structure.Servers;
            var filteredServers = new List<ServerSerializable>();
            bool shouldOverwrite = false;
            foreach (var s in servers)
            {
                var filePath = ServerFilePath(new IPv4Address(s.IpAddress), new Port(s.Port));
                if (!File.Exists(filePath))
                {
                    shouldOverwrite = true;
                    continue;
                }
                filteredServers.Add(s);
            }
            if (shouldOverwrite)
            {
                structure.Servers = filteredServers;
                var saveStatus = Save(structure);
                if (saveStatus.Code != 0)
                    return saveStatus.Prepend(-4, d["Error occured while"], d["deleting"],
                        d["servers"], d["not having"], d["files"]); // -4
            }

            // usuwamy pliki i katalogi z katalogu użytkownika, do których nie ma w BSONie serwerów
            servers = structure.Servers;
            var hashSet = new HashSet<string>();
            foreach (var s in servers)
                hashSet.Add(ServerFileName(new IPv4Address(s.IpAddress), new Port(s.Port)));
            DirectoryInfo di;
            try
            { di = new DirectoryInfo(user.DirectoryPath); }
            catch (Exception)
            {
                return new Status(-5, null, d["Error occured while"],
                    d["listing files and subdirectories in directory"],
                    $"'{user.DirectoryPath}'"); // -5
            }
            foreach (FileInfo file in di.GetFiles())
            {
                if (!hashSet.Contains(file.Name) && file.Name != SERVERS_BSON)
                {
                    try { file.Delete(); }
                    catch (Exception)
                    {
                        return new Status(-6, null, d["Error occured while"],
                            d["deleting"], d["file"], file.Name); // -6
                    }
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                try { dir.Delete(true); }
                catch (Exception)
                {
                    return new Status(-7, null, d["Error occured while"],
                        d["deleting"], d["directory"], dir.Name); // -7
                }
            }
            return new Status(0, structure); // 0
        }

        private Status Load()
        {
            var ensureStatus = EnsureValidDatabaseState();
            if (ensureStatus.Code != 0)
                return ensureStatus.Prepend(-1, d["Error occured while"],
                    d["ensuring valid server database state."]); // -1
            return ensureStatus; // 0
        }

        private Status Save(BsonStructure servers) => Save(serversBsonPath, servers);

        // ':' nie może być w nazwie pliku w NTFS, dlatego jako łącznika używamy '_'
        private string ServerFileName(IPv4Address ipAddress, Port port) =>
            $"{ipAddress}_{port}";

        private string ServerFilePath(IPv4Address ipAddress, Port port) =>
            Path.Combine(user.DirectoryPath, ServerFileName(ipAddress, port));

        public Status Add(Server server)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            var servers = structure.Servers;
            var ipAddress = server.IpAddress;
            var port = server.Port;
            if (Exists(ipAddress, port, servers))
                return ServerAlreadyExistsStatus(-2, ipAddress, port); // -2

            var filePath = ServerFilePath(ipAddress, port);
            if (File.Exists(filePath))
                return ServerFileAlreadyExistsStatus(-3, ipAddress, port); // -3

            servers.Add(server.ToSerializable());
            var saveStatus = Save(structure);
            if (saveStatus.Code != 0)
                return saveStatus.Prepend(-4, d["Error occured while"],
                    d["saving"], d["user's server database file."]); // -4

            try
            /* jeżeli użyjemy tu zwykłego File.Create, to SQLite nie chce się łączyć
            z tak utworzonym plikiem */
            { SQLiteConnection.CreateFile(filePath); }
            catch (Exception)
            {
                var createFileStatus = new Status(-5, null, d["Error occured while"],
                    d["creating"], d["server's file."]);

                servers.RemoveAt(servers.Count - 1); // usuwamy ostatnio dodanego
                saveStatus = Save(structure);
                if (saveStatus.Code != 0)
                    return createFileStatus.Append(-6, d["Error occured while"],
                        d["deleting"], d["the newly-added"] + d["server."], saveStatus.Message); // -6
                return createFileStatus; // -5
            }

            var resetStatus = new ServerDatabase(filePath).ResetDatabase();
            if (resetStatus.Code != 0)
            {
                resetStatus.Prepend(-7, d["Error occured while"],
                    d["resetting server database."]); // -7

                servers.RemoveAt(servers.Count - 1); // usuwamy ostatnio dodanego
                saveStatus = Save(structure);
                if (saveStatus.Code != 0)
                    resetStatus.Append(-8, d["Error occured while"],
                        d["deleting"], d["the newly-added"], d["server."], saveStatus.Message); // -8

                try { File.Delete(filePath); }
                catch (Exception)
                {
                    resetStatus.Append(-9, d["Error occured while"],
                        d["deleting"], d["the newly-added server's database."]); // -9
                }
                return resetStatus;
            }

            return new Status(0);
        }

        public Status GetAll()
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var servers = structure.Servers;

            var observableServers = new List<Server>(servers.Count);
            foreach (var ss in servers)
                observableServers.Add(ss.ToObservable());
            return new Status(0, observableServers); // 0
        }

        private bool Exists(IPv4Address ipAddress, Port port, List<ServerSerializable> servers)
        {
            for (int i = 0; i < servers.Count; ++i)
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    return true;
            return false;
        }

        public Status Exists(IPv4Address ipAddress, Port port)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            if (Exists(ipAddress, port, structure.Servers))
                return ServerAlreadyExistsStatus(0, ipAddress, port); // 0
            else
                return ServerDoesNotExistStatus(1, ipAddress, port); // 1
        }

        public Status Get(IPv4Address ipAddress, Port port)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var servers = structure.Servers;

            for (int i = 0; i < servers.Count; ++i)
            {
                var s = servers[i];
                if (s.KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    return new Status(0, s.ToObservable()); // 0
            }
            return ServerDoesNotExistStatus(-2, ipAddress, port); // -2
        }

        public Status Update(IPv4Address ipAddress, Port port, Server server)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var servers = structure.Servers;

            /*  var oldFileExistsStatus = FileExists(ipAddress, port);
            if (oldFileExistsStatus.Code == 1)
                return oldFileExistsStatus.Prepend(-9); */

            // w obiekcie server może być nowa para (adres IP, port), ale nie może być zajęta
            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                {
                    var serverSerializable = server.ToSerializable();
                    var newIpAddress = server.IpAddress;
                    var newPort = server.Port;
                    int j;
                    for (j = 0; j < i; ++j)
                        if (servers[j].Equals(serverSerializable))
                            return ServerAlreadyExistsStatus(-2, newIpAddress, newPort); // -2
                    for (j = i + 1; j < servers.Count; ++j)
                        if (servers[j].Equals(serverSerializable))
                            return ServerAlreadyExistsStatus(-2, newIpAddress, newPort); // -2

                    string[] dbSaveError = { d["Error occured while"],
                        d["saving"], d["user's server database file."] };
                    if (serverSerializable.KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    {
                        // jeżeli metodą Update nie zmieniamy pary (adres IP, port) serwera
                        servers[i] = serverSerializable;
                        var saveStatus = Save(structure);
                        if (saveStatus.Code != 0)
                            return saveStatus.Prepend(-3, dbSaveError); // -3
                    }
                    else
                    {
                        var newFilePath = ServerFilePath(newIpAddress, newPort);
                        if (File.Exists(newFilePath))
                            return ServerFileAlreadyExistsStatus(-4, newIpAddress, newPort); // -4

                        var oldServerBackup = servers[i];
                        servers[i] = serverSerializable;
                        var saveStatus = Save(structure);
                        if (saveStatus.Code != 0)
                            return saveStatus.Prepend(-5, dbSaveError); // -5

                        var oldFilePath = ServerFilePath(ipAddress, port);
                        if (File.Exists(oldFilePath))
                        {
                            try
                            { File.Move(oldFilePath, newFilePath); }
                            catch (Exception)
                            {
                                var renameFileStatus = new Status(-6, null, d["Error occured while"],
                                    d["renaming"], d["server's file."]); // -6

                                servers[i] = oldServerBackup; // przywracamy ostatnio zastąpionego
                                saveStatus = Save(structure);
                                if (saveStatus.Code != 0)
                                    return renameFileStatus.Append(-7, d["Error occured while"],
                                        d["restoring the updated"], d["server."],
                                        saveStatus.Message); // -7
                                return renameFileStatus; // -6
                            }
                        }
                    }
                    return new Status(0); // 0
                }
            }
            return ServerDoesNotExistStatus(-8, ipAddress, port); // -8
        }

        public Status Delete(IPv4Address ipAddress, Port port)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var servers = structure.Servers;

            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                {
                    var serverBackup = servers[i];
                    servers.RemoveAt(i);
                    var saveStatus = Save(structure);
                    if (saveStatus.Code != 0)
                        return saveStatus.Prepend(-2, d["Error occured while"],
                            d["saving"], d["user's server database file."]); // -2

                    var filePath = ServerFilePath(ipAddress, port);
                    if (File.Exists(filePath))
                    {
                        try
                        { File.Delete(filePath); }
                        catch (Exception)
                        {
                            var deleteFileStatus = new Status(-3, null, d["Error occured while"],
                                d["deleting"], d["server's file."]); // -3

                            servers.Insert(i, serverBackup); // przywracamy ostatnio usuniętego
                            saveStatus = Save(structure);
                            if (saveStatus.Code != 0)
                                return deleteFileStatus.Append(-4, d["Error occured while"],
                                    d["restoring the deleted"], d["server."], saveStatus.Message); // -4
                            return deleteFileStatus; // -3
                        }
                    }
                    return new Status(0); // 0
                }
            }
            return ServerDoesNotExistStatus(-5, ipAddress, port); // -5
        }

        public Status GetServerDatabase(IPv4Address ipAddress, Port port)
        {
            var existsStatus = Exists(ipAddress, port);
            if (existsStatus.Code < 0)
                return existsStatus.Prepend(-1, d["Error occured while"], d["checking if"],
                    d["server"], d["already exists"]); // -1
            if (existsStatus.Code == 1)
                return existsStatus.Prepend(-2); // -2

            var dbPath = ServerFilePath(ipAddress, port);

            return new Status(0, new ServerDatabase(dbPath)); // 0
        }
    }
}
