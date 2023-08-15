using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Shared.MVVM.Model.SQLiteStorage
{
    public abstract class SQLiteDatabase
    {
        protected readonly string _path;

        protected SQLiteDatabase(string path)
        {
            _path = path;
        }

        protected Error FileDoesNotExistError() =>
            new Error($"|Database file| {_path} |does not exist.|");

        protected SQLiteConnection CreateConnection()
        {
            var connectionString = $"Data Source={_path}; Version=3; New=True; Compress=True; " +
                $"foreign keys=true; Journal Mode=Off";
            return new SQLiteConnection(connectionString, true);
        }

        protected void ResetDatabase()
        {
            string ddl = ReadEmbeddedResource(DDLEmbeddedResource());
            if (ddl == null)
                throw new KeyNotFoundException(
                    "Embedded resource with database DDL code does not exist.");
            
            try
            {
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(ddl, con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return;
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    "|executing DDL query creating database schema|.");
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
