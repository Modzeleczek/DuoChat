﻿using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Shared.MVVM.Model.SQLiteStorage
{
    public abstract class SQLiteDatabase : ISQLiteConnector
    {
        protected readonly string _path;

        protected SQLiteDatabase(string path)
        {
            _path = path;

            CreateOrValidateFile();
        }

        public SQLiteConnection CreateConnection()
        {
            // https://stackoverflow.com/a/22297821
            var csb = new SQLiteConnectionStringBuilder();
            csb.DataSource = _path;
            csb.Version = 3;
            csb.ForeignKeys = true;
            csb.JournalMode = SQLiteJournalModeEnum.Off;
            return new SQLiteConnection(csb.ToString());
        }

        protected void RecreateSchema()
        {
            string? ddl = ReadEmbeddedResource(DDLEmbeddedResource());
            if (ddl is null)
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

        private string? ReadEmbeddedResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            using (Stream? stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private void CreateOrValidateFile()
        {
            if (File.Exists(_path))
            {
                ValidateDatabase();
                return;
            }

            /* Plik bazy danych jeszcze nie istnieje i musi zostać utworzony. Trzeba go
            tworzyć za pomocą SQLiteConnection.CreateFile, bo po utworzeniu File.Create
            próba otwarcia na nim SQLiteConnection.Open wyrzuca wyjątek
            System.Data.SQLite.SQLiteException: 'unable to open database file'. */
            try { SQLiteConnection.CreateFile(_path); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    $"|creating| |server's SQLite file| '{_path}'.");
            }

            try { RecreateSchema(); }
            catch (Error createSchemaError)
            {
                createSchemaError.Prepend("|Could not| " +
                    "|create| |server's SQLite schema|.");

                try { File.Delete(_path); }
                catch (Exception deleteException)
                {
                    /* TODO: w sytuacji "undo" Errorów, czyli np. jak
                    tu, że tworzymy plik i nie uda się go zainicjalizować
                    danymi, można w obiekcie createSchemaError, czyli
                    "pierwotnego" błędu, trzymać referencje do kolejnych
                    "łańcuchowych" błędów. */
            var deleteError = new Error(deleteException,
                        "|Error occured while| |deleting| " +
                        $"|server's SQLite file| '{_path}'.");
                    createSchemaError.Append(deleteError.Message);
                }
                throw;
            }
        }

        protected void ValidateDatabase()
        {
            try
            {
                var query = "PRAGMA integrity_check;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    // cmd.CommandType = System.Data.CommandType.Text; // w SQLite dostępny jest tylko typ polecenia Text
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()
                            && reader.GetString(0) == "ok") // (string)reader["integrity_check"]
                        {
                            // w wyniku zapytania nie może być więcej niż tylko 1 rekord "ok"
                            if (!reader.Read())
                                return;
                            else
                                throw new Error("|SQLite integrity check returned " +
                                    "more than single 'ok' row.|");
                        }
                        throw new Error("|SQLite integrity check returned no rows.|");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    "|executing SQLite integrity check.|");
            }
        }
    }
}
