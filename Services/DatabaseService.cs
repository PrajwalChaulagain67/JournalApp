using Microsoft.Data.Sqlite;
using JournalApp.Models;
using System.Data;
using System.IO;

namespace JournalApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "journal.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Users table
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    PinHash TEXT,
                    CreatedAt TEXT NOT NULL
                )";

            // JournalEntries table
            var createEntriesTable = @"
                CREATE TABLE IF NOT EXISTS JournalEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL UNIQUE,
                    Content TEXT NOT NULL,
                    Category TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT
                )";

            // Moods table
            var createMoodsTable = @"
                CREATE TABLE IF NOT EXISTS Moods (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    JournalEntryId INTEGER NOT NULL,
                    Type INTEGER NOT NULL,
                    IsPrimary INTEGER NOT NULL,
                    FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id) ON DELETE CASCADE
                )";

            // Tags table
            var createTagsTable = @"
                CREATE TABLE IF NOT EXISTS Tags (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    JournalEntryId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id) ON DELETE CASCADE
                )";

            using (var command = new SqliteCommand(createUsersTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createEntriesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createMoodsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createTagsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Lightweight migration for older DBs (adds Category column if missing)
            EnsureColumnExists(connection, "JournalEntries", "Category", "TEXT");
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private static void EnsureColumnExists(SqliteConnection connection, string table, string column, string typeSql)
        {
            using var cmd = new SqliteCommand($"PRAGMA table_info({table});", connection);
            using var reader = cmd.ExecuteReader();

            var hasColumn = false;
            while (reader.Read())
            {
                // PRAGMA table_info: name is at index 1
                var name = reader.GetString(1);
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }

            if (hasColumn)
                return;

            using var alter = new SqliteCommand($"ALTER TABLE {table} ADD COLUMN {column} {typeSql};", connection);
            alter.ExecuteNonQuery();
        }
    }
}
