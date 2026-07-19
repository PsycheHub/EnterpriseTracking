using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnterpriseTrackingActivityAgent.Services
{
    /// <summary>
    /// Manages agent login sessions. Logs in once per day via the backend and
    /// persists the JWT + UserId in a local SQLite database so that restarts
    /// within the same day reuse the stored session.
    /// </summary>
    public class SessionManager
    {
        private readonly ILogger<SessionManager> _logger;
        private readonly string _dbPath;
        private const string LoginEndpoint = "/api/account/agent/user/login";

        public string? CurrentUserId { get; private set; }
        public string? CurrentToken { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentUserId) && !string.IsNullOrEmpty(CurrentToken);

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger;
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EnterpriseTrackingAgent",
                "session.db");

            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            InitDb();
        }

        // ── Database helpers ──────────────────────────────────────────────────

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private void InitDb()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS AgentSession (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId      TEXT    NOT NULL,
                    Token       TEXT    NOT NULL,
                    LoginDate   TEXT    NOT NULL,
                    ExpiresAt   TEXT    NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        private void SaveSession(string userId, string token, DateTime expiresAt)
        {
            using var conn = OpenConnection();

            // Keep only the latest session
            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM AgentSession";
                del.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO AgentSession (UserId, Token, LoginDate, ExpiresAt)
                VALUES ($userId, $token, $loginDate, $expiresAt);
                """;
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$token", token);
            cmd.Parameters.AddWithValue("$loginDate", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$expiresAt", expiresAt.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        private (string userId, string token, DateTime loginDate, DateTime expiresAt)? LoadSession()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UserId, Token, LoginDate, ExpiresAt FROM AgentSession LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return (
                reader.GetString(0),
                reader.GetString(1),
                DateTime.Parse(reader.GetString(2)),
                DateTime.Parse(reader.GetString(3))
            );
        }

        // ── Session logic ─────────────────────────────────────────────────────

        /// <summary>
        /// Always performs a fresh login with the supplied credentials and saves
        /// the resulting session locally. Returns false when the server rejects
        /// the credentials.
        /// </summary>
        public async Task<bool> EnsureSessionAsync(string username, string password, string? voucher)
        {
            return await LoginAsync(username, password, voucher);
        }

        private async Task<bool> LoginAsync(string username, string password, string? voucher)
        {
            try
            {
                var options = new RestClientOptions("https://enterprisetracking.onrender.com/");
                using var client = new RestClient(options);

                var body = new { Username = username, Password = password, Voucher = voucher };
                var request = new RestRequest(LoginEndpoint, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddStringBody(JsonSerializer.Serialize(body), DataFormat.Json);

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogError("Login failed. Status={Status}, Body={Body}", response.StatusCode, response.Content);
                    return false;
                }

                var result = JsonSerializer.Deserialize<LoginResponse>(response.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Result == null || string.IsNullOrEmpty(result.Result.Jwt))
                {
                    _logger.LogError("Login returned empty JWT. Response={Response}", response.Content);
                    return false;
                }

                var expiresAt = DateTime.UtcNow.AddDays(2);
                SaveSession(result.Result.UserId, result.Result.Jwt, expiresAt);

                CurrentUserId = result.Result.UserId;
                CurrentToken = result.Result.Jwt;

                _logger.LogInformation("Login successful. UserId={UserId}", CurrentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login");
                return false;
            }
        }

        // ── DTOs for deserialization ──────────────────────────────────────────

        private class LoginResponse
        {
            [JsonPropertyName("statusCode")]
            public int StatusCode { get; set; }

            [JsonPropertyName("result")]
            public LoginResultPayload? Result { get; set; }

            [JsonPropertyName("errorMessages")]
            public List<string>? ErrorMessages { get; set; }
        }

        private class LoginResultPayload
        {
            [JsonPropertyName("jwt")]
            public string Jwt { get; set; } = string.Empty;

            [JsonPropertyName("userId")]
            public string UserId { get; set; } = string.Empty;
        }
    }
}
