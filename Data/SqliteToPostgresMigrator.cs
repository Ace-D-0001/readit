using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Read_It.Data
{
    public static class SqliteToPostgresMigrator
    {
        private static readonly string[] MigrationTables = new[]
        {
            "AspNetRoles",
            "AspNetUsers",
            "AspNetUserRoles",
            "AspNetUserClaims",
            "AspNetRoleClaims",
            "AspNetUserLogins",
            "AspNetUserTokens",
            "Courses",
            "CourseResources",
            "CourseVideos",
            "CourseFollows",
            "CourseMemberships",
            "Posts",
            "Comments",
            "Votes",
            "PostBookmarks",
            "Notifications",
            "Reports",
            "AdminLogs",
            "UserWarnings"
        };

        public static async Task<bool> MigrateAsync(IServiceProvider services, string? sqliteDbPath = null)
        {
            sqliteDbPath ??= Path.Combine(Directory.GetCurrentDirectory(), "app.db");
            if (!File.Exists(sqliteDbPath))
            {
                Console.WriteLine($"[Migrator] SQLite database not found at {sqliteDbPath}. Skipping migration.");
                return false;
            }

            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Console.WriteLine("[Migrator] Ensuring PostgreSQL schema exists...");
            await dbContext.Database.EnsureCreatedAsync();

            var pgConnectionString = dbContext.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(pgConnectionString))
            {
                Console.WriteLine("[Migrator] ERROR: PostgreSQL connection string is empty.");
                return false;
            }

            var sqliteConnStr = $"Data Source={sqliteDbPath}";

            await using var pgConn = new NpgsqlConnection(pgConnectionString);
            await pgConn.OpenAsync();

            await using var sqliteConn = new SqliteConnection(sqliteConnStr);
            await sqliteConn.OpenAsync();

            Console.WriteLine("[Migrator] Connected to both SQLite and PostgreSQL. Starting migration...");

            var summary = new Dictionary<string, (int sqliteRows, int pgRows)>();

            foreach (var tableName in MigrationTables)
            {
                    // Check if table exists in SQLite
                    bool sqliteTableExists;
                    await using (var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@t;", sqliteConn))
                    {
                        checkCmd.Parameters.AddWithValue("@t", tableName);
                        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        sqliteTableExists = count > 0;
                    }

                    if (!sqliteTableExists)
                    {
                        continue;
                    }

                    // Get column metadata from PostgreSQL
                    var pgColumns = new Dictionary<string, (string dataType, string udtName)>(StringComparer.OrdinalIgnoreCase);
                    await using (var colCmd = new NpgsqlCommand(
                        "SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_schema = 'public' AND LOWER(table_name) = LOWER(@t);", pgConn))
                    {
                        colCmd.Parameters.AddWithValue("@t", tableName);
                        await using var reader = await colCmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            var colName = reader.GetString(0);
                            var dataType = reader.GetString(1);
                            var udtName = reader.GetString(2);
                            pgColumns[colName] = (dataType, udtName);
                        }
                    }

                    if (pgColumns.Count == 0)
                    {
                        Console.WriteLine($"[Migrator] Skipping {tableName}: not found in PostgreSQL schema.");
                        continue;
                    }

                    // Read data from SQLite
                    var sqliteRows = 0;
                    var insertedRows = 0;

                    var orderSql = pgColumns.ContainsKey("Id") ? " ORDER BY \"Id\" ASC" : "";
                    await using (var selectCmd = new SqliteCommand($"SELECT * FROM \"{tableName}\"{orderSql};", sqliteConn))
                    await using (var reader = await selectCmd.ExecuteReaderAsync())
                    {
                        var colsInReader = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var colName = reader.GetName(i);
                            if (pgColumns.ContainsKey(colName))
                            {
                                colsInReader.Add(colName);
                            }
                        }

                        if (colsInReader.Count == 0)
                        {
                            continue;
                        }

                        var colListSql = string.Join(", ", colsInReader.Select(c => $"\"{c}\""));
                        var paramListSql = string.Join(", ", colsInReader.Select((c, idx) => $"@p{idx}"));
                        var insertSql = $"INSERT INTO \"{tableName}\" ({colListSql}) VALUES ({paramListSql}) ON CONFLICT DO NOTHING;";

                        while (await reader.ReadAsync())
                        {
                            sqliteRows++;
                            await using var insertCmd = new NpgsqlCommand(insertSql, pgConn);
                            for (int i = 0; i < colsInReader.Count; i++)
                            {
                                var colName = colsInReader[i];
                                var (dataType, udtName) = pgColumns[colName];
                                var rawVal = reader[colName];
                                var convertedVal = ConvertValue(rawVal, dataType, udtName);
                                insertCmd.Parameters.AddWithValue($"@p{i}", convertedVal ?? DBNull.Value);
                            }
                            var affected = await insertCmd.ExecuteNonQueryAsync();
                            if (affected > 0)
                            {
                                insertedRows++;
                            }
                        }
                    }

                    // Count total rows in PostgreSQL
                    int totalPgRows = 0;
                    await using (var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{tableName}\";", pgConn))
                    {
                        totalPgRows = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    summary[tableName] = (sqliteRows, totalPgRows);
                    Console.WriteLine($"[Migrator] {tableName,-20}: SQLite={sqliteRows,4} | PG Total={totalPgRows,4} (+{insertedRows} new)");

                    // Reset auto-increment sequence for tables with integer "Id"
                    if (pgColumns.ContainsKey("Id"))
                    {
                        var resetSeqSql = $@"
DO $$
DECLARE
    max_id integer;
    seq_name text;
BEGIN
    seq_name := pg_get_serial_sequence('""{tableName}""', 'Id');
    IF seq_name IS NOT NULL THEN
        EXECUTE 'SELECT MAX(""Id"") FROM ""{tableName}""' INTO max_id;
        IF max_id IS NOT NULL THEN
            PERFORM setval(seq_name, max_id, true);
        ELSE
            PERFORM setval(seq_name, 1, false);
        END IF;
    END IF;
END $$;
";
                        await using var resetCmd = new NpgsqlCommand(resetSeqSql, pgConn);
                        await resetCmd.ExecuteNonQueryAsync();
                    }
                }

            Console.WriteLine("\n[Migrator] ================= DATA MIGRATION SUMMARY =================");
            foreach (var kvp in summary)
            {
                var match = kvp.Value.sqliteRows == kvp.Value.pgRows ? "MATCH" : "DIFF";
                Console.WriteLine($"[Migrator] {kvp.Key,-20} : SQLite={kvp.Value.sqliteRows,4} | PG={kvp.Value.pgRows,4} [{match}]");
            }
            Console.WriteLine("[Migrator] =========================================================\n");

            return true;
        }

        private static object? ConvertValue(object? sqliteValue, string pgDataType, string pgUdtName)
        {
            if (sqliteValue == null || sqliteValue == DBNull.Value)
            {
                return DBNull.Value;
            }

            var strVal = sqliteValue.ToString() ?? "";
            if (string.IsNullOrEmpty(strVal) && pgDataType != "text" && !pgDataType.Contains("char"))
            {
                return DBNull.Value;
            }

            switch (pgDataType.ToLowerInvariant())
            {
                case "boolean":
                    if (sqliteValue is bool b) return b;
                    if (sqliteValue is long l) return l != 0;
                    if (sqliteValue is int i) return i != 0;
                    if (strVal == "1" || strVal.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                    return false;

                case "timestamp with time zone":
                case "timestamp without time zone":
                case "date":
                    if (DateTime.TryParse(strVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                    if (DateTime.TryParse(strVal, out var dtFallback))
                    {
                        return DateTime.SpecifyKind(dtFallback, DateTimeKind.Utc);
                    }
                    return DBNull.Value;

                case "integer":
                case "smallint":
                    return Convert.ToInt32(sqliteValue);

                case "bigint":
                    return Convert.ToInt64(sqliteValue);

                case "numeric":
                case "decimal":
                case "real":
                case "double precision":
                    return Convert.ToDouble(sqliteValue);

                default:
                    return sqliteValue;
            }
        }
    }
}
