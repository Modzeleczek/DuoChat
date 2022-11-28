using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Client.MVVM.Model
{
    public class DataAccessObject
    {
        public const string DATABASES_DIRECTORY_PATH = "databases";

        private string path;

        public DataAccessObject(string path)
        {
            this.path = path;
        }

        private SQLiteConnection CreateConnection()
        {
            var conStr = $"Data Source={path}; Version = 3; New = True; Compress = True;";
            return new SQLiteConnection(conStr);
        }

        public bool DatabaseFileExists() => File.Exists(path);

        public void DeleteDatabaseFile() => File.Delete(path);

        public void RenameDatabaseFile(string newPath)
        {
            string oldPath = path;
            path = newPath;
            File.Move(oldPath, newPath);
        }

        // https://stackoverflow.com/a/3314213/14357934
        private string ReadEmbeddedResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var all = assembly.GetManifestResourceNames();
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            using (Stream stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null) return null;
                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        public void InitializeDatabaseFile()
        {
            string ddl = ReadEmbeddedResource("Client.MVVM.Model.client.sql");
            if (ddl == null)
                throw new KeyNotFoundException(
                    $"Embedded assembly with client database SQL code does not exist.");
            SQLiteConnection.CreateFile(path);
            using (var con = CreateConnection())
            {
                using (var cmd = new SQLiteCommand(ddl, con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool DatabaseFileHealthy()
        {
            if (!File.Exists(path)) return false;
            string q = "PRAGMA integrity_check;";
            using (var con = CreateConnection())
            using (var cmd = new SQLiteCommand(q, con))
            {
                // cmd.CommandType = System.Data.CommandType.Text; // w SQLite dostępny jest tylko typ polecenia Text
                con.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    if (reader.GetString(0) == "ok") // (string)reader["integrity_check"]
                    {
                        // w wyniku zapytania nie może być więcej niż tylko 1 rekord "ok"
                        if (!reader.Read())
                            return true;
                    }
                }
                return false;
            }
        }
    }
}
