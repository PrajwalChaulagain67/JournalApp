using Microsoft.Data.Sqlite;
using JournalApp.Models;
using JournalApp.Helpers;

namespace JournalApp.Services
{
    public class AuthService
    {
        private readonly DatabaseService _databaseService;

        public AuthService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public bool CreateUser(string username, string password, string? pin = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                var passwordHash = PasswordHasher.HashPassword(password);
                var pinHash = pin != null ? PasswordHasher.HashPassword(pin) : null;

                var command = new SqliteCommand(
                    "INSERT INTO Users (Username, PasswordHash, PinHash, CreatedAt) VALUES (@username, @passwordHash, @pinHash, @createdAt)",
                    connection);

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.Parameters.AddWithValue("@pinHash", pinHash ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("O"));

                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint
            {
                return false;
            }
        }

        public User? Authenticate(string username, string password)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            var command = new SqliteCommand(
                "SELECT * FROM Users WHERE Username = @username",
                connection);
            command.Parameters.AddWithValue("@username", username);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var idOrdinal = reader.GetOrdinal("Id");
                var usernameOrdinal = reader.GetOrdinal("Username");
                var passwordHashOrdinal = reader.GetOrdinal("PasswordHash");
                var pinHashOrdinal = reader.GetOrdinal("PinHash");
                var createdAtOrdinal = reader.GetOrdinal("CreatedAt");

                var storedHash = reader.GetString(passwordHashOrdinal);
                if (PasswordHasher.VerifyPassword(password, storedHash))
                {
                    return new User
                    {
                        Id = reader.GetInt32(idOrdinal),
                        Username = reader.GetString(usernameOrdinal),
                        PasswordHash = storedHash,
                        PinHash = reader.IsDBNull(pinHashOrdinal) ? null : reader.GetString(pinHashOrdinal),
                        CreatedAt = DateTime.Parse(reader.GetString(createdAtOrdinal))
                    };
                }
            }

            return null;
        }

        public bool VerifyPin(User user, string pin)
        {
            if (string.IsNullOrEmpty(user.PinHash))
                return false;

            return PasswordHasher.VerifyPassword(pin, user.PinHash);
        }

        public bool UserExists()
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            var command = new SqliteCommand("SELECT COUNT(*) FROM Users", connection);
            var count = Convert.ToInt64(command.ExecuteScalar());
            return count > 0;
        }
    }
}
