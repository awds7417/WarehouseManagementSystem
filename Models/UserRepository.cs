using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WarehouseManagementSystem.Models
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository()
        {
            _connectionString = ConfigurationManager
                .ConnectionStrings["WarehouseDb"]
                .ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public User GetByUserName(string userName)
        {
            User user = null;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
SELECT Id, UserName, PasswordHash, DisplayName, IsAdmin
FROM dbo.Users
WHERE UserName = @UserName";

                cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = userName;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new User();
                        int i = 0;
                        user.Id = reader.GetInt32(i++);
                        user.UserName = reader.GetString(i++);
                        user.PasswordHash = reader.GetString(i++);
                        user.DisplayName = reader.IsDBNull(i) ? null : reader.GetString(i);
                        i++;
                        user.IsAdmin = reader.GetBoolean(i++);
                    }
                }
            }

            return user;
        }

        public void Insert(User user)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
INSERT INTO dbo.Users (UserName, PasswordHash, DisplayName, IsAdmin)
VALUES (@UserName, @PasswordHash, @DisplayName, @IsAdmin);";

                cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = user.UserName;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 200).Value = user.PasswordHash;

                if (user.DisplayName == null)
                    cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 50).Value = user.DisplayName;

                cmd.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = user.IsAdmin;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
