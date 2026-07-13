using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Hosting;

namespace YourProject
{
    public static class DatabaseBootstrap
    {
        private static readonly string[] ScriptFiles =
        {
            "init-schema.sql",
            "migrate-voorraad-locatie.sql",
            "migrate-barcodes-soort.sql",
            "migrate-alarm-per-bak.sql"
        };

        public static void EnsureDatabase()
        {
            var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connString))
            {
                Trace.TraceWarning("DatabaseBootstrap: geen DefaultConnection in Web.config.");
                return;
            }

            try
            {
                EnsureDatabaseExists(connString);

                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();
                    foreach (var file in ScriptFiles)
                        ExecuteScriptFile(conn, file);
                }

                Trace.TraceInformation("DatabaseBootstrap: schema gecontroleerd.");
            }
            catch (Exception ex)
            {
                Trace.TraceError("DatabaseBootstrap mislukt: " + ex);
            }
        }

        private static void EnsureDatabaseExists(string connString)
        {
            var builder = new SqlConnectionStringBuilder(connString);
            var databaseName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("Connection string heeft geen database naam.");

            builder.InitialCatalog = "master";
            using (var conn = new SqlConnection(builder.ConnectionString))
            using (var cmd = new SqlCommand(
                "IF DB_ID(@Name) IS NULL EXEC('CREATE DATABASE [' + REPLACE(@Name, ']', ']]') + ']')", conn))
            {
                cmd.Parameters.AddWithValue("@Name", databaseName);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecuteScriptFile(SqlConnection conn, string fileName)
        {
            var path = HostingEnvironment.MapPath("~/Scripts/" + fileName);
            if (path == null || !File.Exists(path))
            {
                Trace.TraceWarning("DatabaseBootstrap: script niet gevonden: " + fileName);
                return;
            }

            var sql = File.ReadAllText(path);
            ExecuteBatches(conn, sql);
        }

        private static void ExecuteBatches(SqlConnection conn, string sql)
        {
            var batches = Regex.Split(sql, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                using (var cmd = new SqlCommand(trimmed, conn))
                {
                    cmd.CommandTimeout = 120;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
