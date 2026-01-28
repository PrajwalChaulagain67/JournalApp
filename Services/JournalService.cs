using Microsoft.Data.Sqlite;
using JournalApp.Models;

namespace JournalApp.Services
{
    public class JournalService
    {
        private readonly DatabaseService _databaseService;

        public JournalService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public JournalEntry? GetEntryByDate(DateTime date)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            var dateStr = date.Date.ToString("yyyy-MM-dd");
            var command = new SqliteCommand(
                "SELECT * FROM JournalEntries WHERE Date = @date",
                connection);
            command.Parameters.AddWithValue("@date", dateStr);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var idOrdinal = reader.GetOrdinal("Id");
                var dateOrdinal = reader.GetOrdinal("Date");
                var contentOrdinal = reader.GetOrdinal("Content");
                var categoryOrdinal = reader.GetOrdinal("Category");
                var createdAtOrdinal = reader.GetOrdinal("CreatedAt");
                var updatedAtOrdinal = reader.GetOrdinal("UpdatedAt");

                var entry = new JournalEntry
                {
                    Id = reader.GetInt32(idOrdinal),
                    Date = DateTime.Parse(reader.GetString(dateOrdinal)),
                    Content = reader.GetString(contentOrdinal),
                    Category = reader.IsDBNull(categoryOrdinal) ? null : reader.GetString(categoryOrdinal),
                    CreatedAt = DateTime.Parse(reader.GetString(createdAtOrdinal)),
                    UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? null : DateTime.Parse(reader.GetString(updatedAtOrdinal))
                };

                entry.Moods = GetMoodsForEntry(entry.Id);
                entry.Tags = GetTagsForEntry(entry.Id);

                return entry;
            }

            return null;
        }

        public void SaveEntry(JournalEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry), "Journal entry cannot be null.");
            }

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var dateStr = entry.Date.Date.ToString("yyyy-MM-dd");
                var existingEntry = GetEntryByDate(entry.Date);

                if (existingEntry != null)
                {
                    // Update existing entry
                    var updateCommand = new SqliteCommand(
                        "UPDATE JournalEntries SET Content = @content, Category = @category, UpdatedAt = @updatedAt WHERE Id = @id",
                        connection, transaction);
                    updateCommand.Parameters.AddWithValue("@content", entry.Content);
                    updateCommand.Parameters.AddWithValue("@category", (object?)entry.Category ?? DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("O"));
                    updateCommand.Parameters.AddWithValue("@id", existingEntry.Id);
                    updateCommand.ExecuteNonQuery();

                    // Delete old moods and tags
                    var deleteMoods = new SqliteCommand("DELETE FROM Moods WHERE JournalEntryId = @id", connection, transaction);
                    deleteMoods.Parameters.AddWithValue("@id", existingEntry.Id);
                    deleteMoods.ExecuteNonQuery();

                    var deleteTags = new SqliteCommand("DELETE FROM Tags WHERE JournalEntryId = @id", connection, transaction);
                    deleteTags.Parameters.AddWithValue("@id", existingEntry.Id);
                    deleteTags.ExecuteNonQuery();

                    entry.Id = existingEntry.Id;
                }
                else
                {
                    // Insert new entry
                    var insertCommand = new SqliteCommand(
                        "INSERT INTO JournalEntries (Date, Content, Category, CreatedAt) VALUES (@date, @content, @category, @createdAt)",
                        connection, transaction);
                    insertCommand.Parameters.AddWithValue("@date", dateStr);
                    insertCommand.Parameters.AddWithValue("@content", entry.Content);
                    insertCommand.Parameters.AddWithValue("@category", (object?)entry.Category ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("O"));
                    insertCommand.ExecuteNonQuery();

                    // Get the last inserted ID
                    var getIdCommand = new SqliteCommand("SELECT last_insert_rowid()", connection, transaction);
                    entry.Id = Convert.ToInt32(getIdCommand.ExecuteScalar());
                }

                // Insert moods
                if (entry.Moods != null)
                {
                    foreach (var mood in entry.Moods)
                    {
                        if (mood != null)
                        {
                            var moodCommand = new SqliteCommand(
                                "INSERT INTO Moods (JournalEntryId, Type, IsPrimary) VALUES (@entryId, @type, @isPrimary)",
                                connection, transaction);
                            moodCommand.Parameters.AddWithValue("@entryId", entry.Id);
                            moodCommand.Parameters.AddWithValue("@type", (int)mood.Type);
                            moodCommand.Parameters.AddWithValue("@isPrimary", mood.IsPrimary ? 1 : 0);
                            moodCommand.ExecuteNonQuery();
                        }
                    }
                }

                // Insert tags
                if (entry.Tags != null)
                {
                    foreach (var tag in entry.Tags)
                    {
                        if (tag != null && !string.IsNullOrWhiteSpace(tag.Name))
                        {
                            var tagCommand = new SqliteCommand(
                                "INSERT INTO Tags (JournalEntryId, Name) VALUES (@entryId, @name)",
                                connection, transaction);
                            tagCommand.Parameters.AddWithValue("@entryId", entry.Id);
                            tagCommand.Parameters.AddWithValue("@name", tag.Name);
                            tagCommand.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteEntry(DateTime date)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            var dateStr = date.Date.ToString("yyyy-MM-dd");
            var command = new SqliteCommand(
                "DELETE FROM JournalEntries WHERE Date = @date",
                connection);
            command.Parameters.AddWithValue("@date", dateStr);
            command.ExecuteNonQuery();
        }

        public List<JournalEntry> GetAllEntries()
        {
            var entries = new List<JournalEntry>();

            using var connection = _databaseService.GetConnection();
            connection.Open();

            var command = new SqliteCommand("SELECT * FROM JournalEntries ORDER BY Date DESC", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var idOrdinal = reader.GetOrdinal("Id");
                var dateOrdinal = reader.GetOrdinal("Date");
                var contentOrdinal = reader.GetOrdinal("Content");
                var categoryOrdinal = reader.GetOrdinal("Category");
                var createdAtOrdinal = reader.GetOrdinal("CreatedAt");
                var updatedAtOrdinal = reader.GetOrdinal("UpdatedAt");

                var entry = new JournalEntry
                {
                    Id = reader.GetInt32(idOrdinal),
                    Date = DateTime.Parse(reader.GetString(dateOrdinal)),
                    Content = reader.GetString(contentOrdinal),
                    Category = reader.IsDBNull(categoryOrdinal) ? null : reader.GetString(categoryOrdinal),
                    CreatedAt = DateTime.Parse(reader.GetString(createdAtOrdinal)),
                    UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? null : DateTime.Parse(reader.GetString(updatedAtOrdinal))
                };

                entry.Moods = GetMoodsForEntry(entry.Id);
                entry.Tags = GetTagsForEntry(entry.Id);

                entries.Add(entry);
            }

            return entries;
        }

        public List<JournalEntry> SearchEntries(string searchTerm)
        {
            var allEntries = GetAllEntries();
            return allEntries.Where(e =>
                e.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Tags.Any(t => t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        public List<JournalEntry> FilterByMood(MoodType moodType)
        {
            var allEntries = GetAllEntries();
            return allEntries.Where(e => e.Moods.Any(m => m.Type == moodType)).ToList();
        }

        public List<JournalEntry> FilterByTag(string tagName)
        {
            var allEntries = GetAllEntries();
            return allEntries.Where(e => e.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        private List<Mood> GetMoodsForEntry(int entryId)
        {
            var moods = new List<Mood>();

            using var connection = _databaseService.GetConnection();
            connection.Open();

            var command = new SqliteCommand(
                "SELECT * FROM Moods WHERE JournalEntryId = @entryId",
                connection);
            command.Parameters.AddWithValue("@entryId", entryId);

            using var reader = command.ExecuteReader();
            var idOrdinal = reader.GetOrdinal("Id");
            var typeOrdinal = reader.GetOrdinal("Type");
            var isPrimaryOrdinal = reader.GetOrdinal("IsPrimary");

            while (reader.Read())
            {
                moods.Add(new Mood
                {
                    Id = reader.GetInt32(idOrdinal),
                    JournalEntryId = entryId,
                    Type = (MoodType)reader.GetInt32(typeOrdinal),
                    IsPrimary = reader.GetInt32(isPrimaryOrdinal) == 1
                });
            }

            return moods;
        }

        private List<Tag> GetTagsForEntry(int entryId)
        {
            var tags = new List<Tag>();

            using var connection = _databaseService.GetConnection();
            connection.Open();

            var command = new SqliteCommand(
                "SELECT * FROM Tags WHERE JournalEntryId = @entryId",
                connection);
            command.Parameters.AddWithValue("@entryId", entryId);

            using var reader = command.ExecuteReader();
            var idOrdinal = reader.GetOrdinal("Id");
            var nameOrdinal = reader.GetOrdinal("Name");

            while (reader.Read())
            {
                tags.Add(new Tag
                {
                    Id = reader.GetInt32(idOrdinal),
                    JournalEntryId = entryId,
                    Name = reader.GetString(nameOrdinal)
                });
            }

            return tags;
        }
    }
}
