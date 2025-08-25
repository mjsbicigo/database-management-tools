using Microsoft.Data.SqlClient;

namespace DatabaseManagementTools.Services
{
    public static class DatabaseService
    {
        public static List<string> ListDatabases(SqlConnection conn)
        {
            var result = new List<string>();

            var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                result.Add(reader.GetString(0));

            return result;
        }
    }
}

