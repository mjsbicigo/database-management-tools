using Microsoft.Data.SqlClient;

namespace DatabaseManagementTools.Services
{
    public class SqlConnectionManager
    {
        private readonly string _connectionString;

        public SqlConnectionManager(string server, string user, string password)
        {
            _connectionString = $"Server={server};User Id={user};Password={password};MultipleActiveResultSets=True;Connect Timeout=10;Encrypt=True;TrustServerCertificate=True";
        }

        public SqlConnection GetOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
