using Client.MVVM.Model.JsonSerializables;
using Client.MVVM.Model.SQLiteStorage;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
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

        public class BsonStructure
        {
            public List<ServerSerializable> Servers { get; set; } = new List<ServerSerializable>();
        }

        private Error ServerAlreadyExistsError(IPv4Address ipAddress, Port port) =>
            new Error($"|Server with IP address| {ipAddress} " +
                $"|and port| {port} |already exists.|");

        private Error ServerDoesNotExistError(IPv4Address ipAddress, Port port) =>
            new Error($"|Server with IP address| {ipAddress} " +
                $"|and port| {port} |does not exist.|");

        private Error ServerFileAlreadyExistsError(IPv4Address ipAddress, Port port) =>
            new Error("|Server's file with name| " +
                $"{ServerFileName(ipAddress, port)} |already exists.|");

        private Error ServerFileDoesNotExistError(IPv4Address ipAddress, Port port) =>
            new Error("|Server's file with name| " +
                $"{ServerFileName(ipAddress, port)} |does not exist.|");

        public BsonStructure EnsureValidDatabaseState()
        {
            var path = user.DirectoryPath;
            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| |creating| " +
                        $"|directory| {path}.");
                }
            }
            path = serversBsonPath;
            if (!File.Exists(path))
            {
                try { Save(new BsonStructure()); }
                catch (Error e)
                {
                    e.Prepend($"|Error occured while| |creating| |file| {path}.");
                    throw;
                }
            }

            var structure = Load<BsonStructure>(path);
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
                try { Save(structure); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |deleting| " +
                        "|servers| |not having| |files|.");
                    throw;
                }
            }

            // usuwamy pliki i katalogi z katalogu użytkownika, do których nie ma w BSONie serwerów
            servers = structure.Servers;
            var hashSet = new HashSet<string>();
            foreach (var s in servers)
                hashSet.Add(ServerFileName(new IPv4Address(s.IpAddress), new Port(s.Port)));
            DirectoryInfo di;
            try { di = new DirectoryInfo(user.DirectoryPath); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    "|listing files and subdirectories in directory| " +
                    $"'{user.DirectoryPath}'.");
            }
            foreach (FileInfo file in di.GetFiles())
            {
                if (!hashSet.Contains(file.Name) && file.Name != SERVERS_BSON)
                {
                    try { file.Delete(); }
                    catch (Exception e)
                    {
                        throw new Error(e, "|Error occured while| " +
                            $"|deleting| |file| {file.Name}.");
                    }
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                try { dir.Delete(true); }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| " +
                        $"|deleting| |directory| {dir.Name}.");
                }
            }
            return structure;
        }

        private BsonStructure Load()
        {
            try { return EnsureValidDatabaseState(); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|ensuring valid server database state.|");
                throw;
            }
        }

        private void Save(BsonStructure servers) => Save(serversBsonPath, servers);

        // ':' nie może być w nazwie pliku w NTFS, dlatego jako łącznika używamy '_'
        private string ServerFileName(IPv4Address ipAddress, Port port) =>
            $"{ipAddress}_{port}";

        private string ServerFilePath(IPv4Address ipAddress, Port port) =>
            Path.Combine(user.DirectoryPath, ServerFileName(ipAddress, port));

        public void Add(Server server)
        {
            var structure = Load();
            var servers = structure.Servers;
            var ipAddress = server.IpAddress;
            var port = server.Port;
            if (Exists(ipAddress, port, servers))
                throw ServerAlreadyExistsError(ipAddress, port);

            var filePath = ServerFilePath(ipAddress, port);
            if (File.Exists(filePath))
                throw ServerFileAlreadyExistsError(ipAddress, port);

            servers.Add(server.ToSerializable());
            try { Save(structure); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|saving| |user's server database file.|");
                throw;
            }

            try { new ServerDatabase(filePath).CreateSQLiteFile(); }
            catch (Error createFileError)
            {
                servers.RemoveAt(servers.Count - 1); // usuwamy ostatnio dodanego
                try { Save(structure); }
                catch (Error saveError)
                {
                    createFileError.Append("|Error occured while| " +
                        "|deleting| |the newly-added| |server.| " + saveError.Message);
                }
                throw createFileError;
            }
        }

        public List<Server> GetAll()
        {
            var servers = Load().Servers;

            var observableServers = new List<Server>(servers.Count);
            foreach (var ss in servers)
                observableServers.Add(ss.ToObservable());
            return observableServers;
        }

        private bool Exists(IPv4Address ipAddress, Port port, List<ServerSerializable> servers)
        {
            for (int i = 0; i < servers.Count; ++i)
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    return true;
            return false;
        }

        public bool Exists(IPv4Address ipAddress, Port port)
        {
            return Exists(ipAddress, port, Load().Servers);
        }

        public Server Get(IPv4Address ipAddress, Port port)
        {
            var servers = Load().Servers;

            for (int i = 0; i < servers.Count; ++i)
            {
                var s = servers[i];
                if (s.KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    return s.ToObservable();
            }
            throw null;
        }

        public void Update(IPv4Address ipAddress, Port port, Server server)
        {
            var structure = Load();
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
                            throw ServerAlreadyExistsError(newIpAddress, newPort);
                    for (j = i + 1; j < servers.Count; ++j)
                        if (servers[j].Equals(serverSerializable))
                            throw ServerAlreadyExistsError(newIpAddress, newPort);

                    string dbSaveError = "|Error occured while| " +
                        "|saving| |user's server database file.|";
                    if (serverSerializable.KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    {
                        // jeżeli metodą Update nie zmieniamy pary (adres IP, port) serwera
                        servers[i] = serverSerializable;
                        try { Save(structure); }
                        catch (Error e)
                        {
                            e.Prepend(dbSaveError);
                            throw;
                        }
                    }
                    else
                    {
                        var newFilePath = ServerFilePath(newIpAddress, newPort);
                        if (File.Exists(newFilePath))
                            throw ServerFileAlreadyExistsError(newIpAddress, newPort);

                        var oldServerBackup = servers[i];
                        servers[i] = serverSerializable;
                        try { Save(structure); }
                        catch (Error e)
                        {
                            e.Prepend(dbSaveError);
                            throw;
                        }

                        var oldFilePath = ServerFilePath(ipAddress, port);
                        if (File.Exists(oldFilePath))
                        {
                            try { File.Move(oldFilePath, newFilePath); }
                            catch (Exception e)
                            {
                                var renameFileError = new Error(e,
                                    "|Error occured while| |renaming| |server's file.|");

                                servers[i] = oldServerBackup; // przywracamy ostatnio zastąpionego
                                try { Save(structure); }
                                catch (Error saveError)
                                {
                                    renameFileError.Append("|Error occured while| " +
                                        "|restoring the updated| |server.| " +
                                        saveError.Message);
                                }
                                throw renameFileError;
                            }
                        }
                    }
                    return;
                }
            }
            throw ServerDoesNotExistError(ipAddress, port);
        }

        public void Delete(IPv4Address ipAddress, Port port)
        {
            var structure = Load();
            var servers = structure.Servers;

            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                {
                    var serverBackup = servers[i];
                    servers.RemoveAt(i);
                    try { Save(structure); }
                    catch (Error e)
                    {
                        e.Prepend("|Error occured while| |saving| " +
                                "|user's server database file.|");
                        throw;
                    }

                    var filePath = ServerFilePath(ipAddress, port);
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); }
                        catch (Exception e)
                        {
                            var deleteFileError = new Error(e, "|Error occured while| " +
                                "|deleting| |server's file.|");

                            servers.Insert(i, serverBackup); // przywracamy ostatnio usuniętego
                            try { Save(structure); }
                            catch (Error saveError)
                            {
                                deleteFileError.Append("|Error occured while| " +
                                    "|restoring the deleted| |server.| " +
                                    saveError.Message);
                            }
                            throw deleteFileError;
                        }
                    }
                    return;
                }
            }
            throw ServerDoesNotExistError(ipAddress, port);
        }

        public ServerDatabase GetServerDatabase(IPv4Address ipAddress, Port port)
        {
            try
            {
                if (!Exists(ipAddress, port))
                    throw ServerDoesNotExistError(ipAddress, port);
            }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |checking if| " +
                    "|server| |already exists.|");
                throw;
            }

            return new ServerDatabase(ServerFilePath(ipAddress, port));
        }
    }
}
