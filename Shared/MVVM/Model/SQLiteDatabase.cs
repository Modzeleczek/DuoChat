using Shared.MVVM.View.Localization;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Shared.MVVM.Model
{
    public abstract class SQLiteDatabase
    {
        protected readonly Translator d = Translator.Instance;
        protected readonly string _path;

        protected SQLiteDatabase(string path)
        {
            _path = path;
        }

        protected Status FileDoesNotExistStatus(int code) =>
            new Status(code, null, d["Database file"], $"{_path}", d["does not exist."]);

        protected SQLiteConnection CreateConnection()
        {
            var connectionString = $"Data Source={_path}; Version=3; New=True; Compress=True; " +
                $"foreign keys=true; Journal Mode=Off";
            return new SQLiteConnection(connectionString, true);
        }

        public Status ResetDatabase()
        {
            string ddl = ReadEmbeddedResource(DDLEmbeddedResource());
            if (ddl == null)
                throw new KeyNotFoundException(
                    "Embedded resource with database DDL code does not exist.");
            if (!File.Exists(_path))
                return FileDoesNotExistStatus(-1);
            try
            {
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(ddl, con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return new Status(0);
                }
            }
            catch (Exception)
            {
                return new Status(-2, null, d["Error occured while"],
                    d["executing DDL query creating database."]); // -2
            }
        }

        protected abstract string DDLEmbeddedResource();

        private string ReadEmbeddedResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            using (Stream stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null) return null;
                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}
