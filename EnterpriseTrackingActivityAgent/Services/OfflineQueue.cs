using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace EnterpriseTrackingActivityAgent.Services
{
    public class OfflineQueue
    {
        private readonly string _conn;

        public OfflineQueue()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EnterpriseTrackingAgent");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var dbPath = Path.Combine(folder, "queue.db");

            _conn = $"Data Source={dbPath}";

            Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_conn);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS queue(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                payload TEXT NOT NULL
            );
            """;

            cmd.ExecuteNonQuery();
        }

        public async Task SaveAsync(object data)
        {
            using var connection = new SqliteConnection(_conn);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            "INSERT INTO queue(payload) VALUES ($payload)";

            cmd.Parameters.AddWithValue("$payload",
                JsonSerializer.Serialize(data));

            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine("Saved to queue");
        }

        public async Task<List<(long Id, string Payload)>> GetAllAsync()
        {
            var list = new List<(long, string)>();

            using var connection = new SqliteConnection(_conn);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, payload FROM queue ORDER BY id";

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add((reader.GetInt64(0), reader.GetString(1)));
            }

            return list;
        }

        public async Task DeleteAsync(long id)
        {
            using var connection = new SqliteConnection(_conn);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM queue WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}