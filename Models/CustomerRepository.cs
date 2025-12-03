using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WarehouseManagementSystem.Models
{
    public class CustomerRepository
    {
        private readonly string _connectionString;
    
        public CustomerRepository()
        {
            _connectionString = ConfigurationManager
                .ConnectionStrings["WarehouseDb"]
                .ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // 1) 取得全部客戶
        public IList<Customer> GetAll()
        {
            var list = new List<Customer>();

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                SELECT Id, CustomerCode, Name, ContactPerson, Phone, Address
                FROM dbo.Customers
                ORDER BY Id";

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Customer c = new Customer();
                        c.Id = reader.GetInt32(0);
                        c.CustomerCode = reader.GetString(1);
                        c.Name = reader.GetString(2);
                        c.ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3);
                        c.Phone = reader.IsDBNull(4) ? null : reader.GetString(4);
                        c.Address = reader.IsDBNull(5) ? null : reader.GetString(5);

                        list.Add(c);
                    }
                }
            }

            return list;
        }

        // 2) 依 Id 取得單一客戶
        public Customer GetById(int id)
        {
            Customer c = null;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                SELECT Id, CustomerCode, Name, ContactPerson, Phone, Address
                FROM dbo.Customers
                WHERE Id = @Id";

                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        c = new Customer();
                        c.Id = reader.GetInt32(0);
                        c.CustomerCode = reader.GetString(1);
                        c.Name = reader.GetString(2);
                        c.ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3);
                        c.Phone = reader.IsDBNull(4) ? null : reader.GetString(4);
                        c.Address = reader.IsDBNull(5) ? null : reader.GetString(5);
                    }
                }
            }

            return c;
        }

        // 3) 新增客戶
        public void Insert(Customer c)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
INSERT INTO dbo.Customers (CustomerCode, Name, ContactPerson, Phone, Address)
VALUES (@CustomerCode, @Name, @ContactPerson, @Phone, @Address);";

                cmd.Parameters.Add("@CustomerCode", SqlDbType.NVarChar, 50).Value = c.CustomerCode;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = c.Name;

                if (c.ContactPerson == null)
                    cmd.Parameters.Add("@ContactPerson", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@ContactPerson", SqlDbType.NVarChar, 50).Value = c.ContactPerson;

                if (c.Phone == null)
                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = c.Phone;

                if (c.Address == null)
                    cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 200).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 200).Value = c.Address;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 4) 更新客戶
        public void Update(Customer c)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                UPDATE dbo.Customers
                SET CustomerCode = @CustomerCode,
                    Name = @Name,
                    ContactPerson = @ContactPerson,
                    Phone = @Phone,
                    Address = @Address
                WHERE Id = @Id;";

                cmd.Parameters.Add("@CustomerCode", SqlDbType.NVarChar, 50).Value = c.CustomerCode;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = c.Name;

                if (c.ContactPerson == null)
                    cmd.Parameters.Add("@ContactPerson", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@ContactPerson", SqlDbType.NVarChar, 50).Value = c.ContactPerson;

                if (c.Phone == null)
                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = c.Phone;

                if (c.Address == null)
                    cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 200).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 200).Value = c.Address;

                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = c.Id;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 5) 刪除客戶
        public void Delete(int id)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "DELETE FROM dbo.Customers WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 6) 製作分頁
        public IList<Customer> GetPaged(int pageIndex, int pageSize, out int totalCount)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize <= 0) pageSize = 6;  // 每頁 6 筆

            int startRow = (pageIndex - 1) * pageSize + 1;
            int endRow = pageIndex * pageSize;

            var list = new List<Customer>();
            totalCount = 0;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                // 1) 先算總筆數
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Customers;";
                conn.Open();

                object countObj = cmd.ExecuteScalar();
                if (countObj != null && countObj != DBNull.Value)
                    totalCount = Convert.ToInt32(countObj);

                if (totalCount == 0)
                    return list;

                // 2) 再取本頁資料（用 ROW_NUMBER 分頁）
                cmd.Parameters.Clear();
                cmd.CommandText = @"
;WITH CustomerCTE AS
(
    SELECT  Id,
            CustomerCode,
            Name,
            ContactPerson,
            Phone,
            Address,
            ROW_NUMBER() OVER (ORDER BY Id) AS RowNum
    FROM dbo.Customers
)
SELECT Id, CustomerCode, Name, ContactPerson, Phone, Address
FROM CustomerCTE
WHERE RowNum BETWEEN @StartRow AND @EndRow
ORDER BY RowNum;";

                cmd.Parameters.Add("@StartRow", SqlDbType.Int).Value = startRow;
                cmd.Parameters.Add("@EndRow", SqlDbType.Int).Value = endRow;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Customer c = new Customer();
                        int i = 0;

                        c.Id = reader.GetInt32(i++);
                        c.CustomerCode = reader.GetString(i++);
                        c.Name = reader.GetString(i++);

                        // ContactPerson
                        if (reader.IsDBNull(i))
                            c.ContactPerson = null;
                        else
                            c.ContactPerson = reader.GetString(i);
                        i++;

                        // Phone
                        if (reader.IsDBNull(i))
                            c.Phone = null;
                        else
                            c.Phone = reader.GetString(i);
                        i++;

                        // Address
                        if (reader.IsDBNull(i))
                            c.Address = null;
                        else
                            c.Address = reader.GetString(i);
                        i++;

                        list.Add(c);

                    }
                }
            }

            return list;
        }
    }
}