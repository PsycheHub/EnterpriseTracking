using Microsoft.Data.Sqlite;

namespace EnterpriseTrackingAuthPipelineService.Cache
{



    public sealed class AuthCacheStore
    {
        private readonly string _connectionString;

        public AuthCacheStore()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EnterpriseTrackingAgent");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var dbPath = Path.Combine(folder, "auth.db");
            _connectionString = $"Data Source={dbPath}";

            Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            """
        CREATE TABLE IF NOT EXISTS auth_cache(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT NOT NULL UNIQUE,
            encrypted_password TEXT NOT NULL,
            encrypted_user_id TEXT NOT NULL,
            encrypted_jwt TEXT NOT NULL,
            encrypted_roles TEXT NOT NULL,
            last_login_utc TEXT NOT NULL
        );
        """;

            cmd.ExecuteNonQuery();
        }

        public async Task UpsertAsync(AuthCacheRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            """
        INSERT INTO auth_cache
        (username, encrypted_password, encrypted_user_id, encrypted_jwt, encrypted_roles, last_login_utc)
        VALUES ($username, $password, $userId, $jwt, $roles, $lastLoginUtc)
        ON CONFLICT(username) DO UPDATE SET
            encrypted_password = excluded.encrypted_password,
            encrypted_user_id = excluded.encrypted_user_id,
            encrypted_jwt = excluded.encrypted_jwt,
            encrypted_roles = excluded.encrypted_roles,
            last_login_utc = excluded.last_login_utc;
        """;

            cmd.Parameters.AddWithValue("$username", record.Username);
            cmd.Parameters.AddWithValue("$password", record.EncryptedPassword);
            cmd.Parameters.AddWithValue("$userId", record.EncryptedUserId);
            cmd.Parameters.AddWithValue("$jwt", record.EncryptedJwt);
            cmd.Parameters.AddWithValue("$roles", record.EncryptedRoles);
            cmd.Parameters.AddWithValue("$lastLoginUtc", record.LastLoginUtc);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<AuthCacheRecord?> FindByUsernameAsync(string username)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            """
        SELECT id, username, encrypted_password, encrypted_user_id, encrypted_jwt, encrypted_roles, last_login_utc
        FROM auth_cache
        WHERE username = $username
        LIMIT 1;
        """;

            cmd.Parameters.AddWithValue("$username", username);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new AuthCacheRecord
            {
                Id = reader.GetInt64(0),
                Username = reader.GetString(1),
                EncryptedPassword = reader.GetString(2),
                EncryptedUserId = reader.GetString(3),
                EncryptedJwt = reader.GetString(4),
                EncryptedRoles = reader.GetString(5),
                LastLoginUtc = reader.GetString(6)
            };
        }
    }
}
