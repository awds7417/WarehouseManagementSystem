using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WarehouseManagementSystem.Models
{
    public class ProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["WarehouseDb"].ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // 1) 取得全部產品
        public IList<Product> GetAll()
        {
            var list = new List<Product>();

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "SELECT Id, ProductCode, Name, Unit, SafeStockQty FROM dbo.Products ORDER BY Id";

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product p = new Product();
                        p.Id = reader.GetInt32(0);
                        p.ProductCode = reader.GetString(1);
                        p.Name = reader.GetString(2);
                        p.Unit = reader.IsDBNull(3) ? null : reader.GetString(3);
                        p.SafeStockQty = reader.GetInt32(4);

                        list.Add(p);
                    }
                }
            }

            return list;
        }

        // 2) 依 Id 取得單一產品
        public Product GetById(int id)
        {
            Product p = null;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "SELECT Id, ProductCode, Name, Unit, SafeStockQty FROM dbo.Products WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        p = new Product();
                        p.Id = reader.GetInt32(0);
                        p.ProductCode = reader.GetString(1);
                        p.Name = reader.GetString(2);
                        p.Unit = reader.IsDBNull(3) ? null : reader.GetString(3);
                        p.SafeStockQty = reader.GetInt32(4);
                    }
                }
            }

            return p;
        }

        // 3) 新增產品
        public void Insert(Product p)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
INSERT INTO dbo.Products (ProductCode, Name, Unit, SafeStockQty)
VALUES (@ProductCode, @Name, @Unit, @SafeStockQty);";

                cmd.Parameters.Add("@ProductCode", SqlDbType.NVarChar, 50).Value = p.ProductCode;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = p.Name;

                if (p.Unit == null)
                {
                    cmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = DBNull.Value;
                }
                else
                {
                    cmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = p.Unit;
                }

                cmd.Parameters.Add("@SafeStockQty", SqlDbType.Int).Value = p.SafeStockQty;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 4) 更新產品
        public void Update(Product p)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
UPDATE dbo.Products
SET ProductCode = @ProductCode,
    Name = @Name,
    Unit = @Unit,
    SafeStockQty = @SafeStockQty
WHERE Id = @Id;";

                cmd.Parameters.Add("@ProductCode", SqlDbType.NVarChar, 50).Value = p.ProductCode;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = p.Name;

                if (p.Unit == null)
                {
                    cmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = DBNull.Value;
                }
                else
                {
                    cmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = p.Unit;
                }

                cmd.Parameters.Add("@SafeStockQty", SqlDbType.Int).Value = p.SafeStockQty;
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = p.Id;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 5) 刪除產品
        public void Delete(int id)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "DELETE FROM dbo.Products WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 5) 搜尋產品
        public IList<Product> Search(string keyword, int pageIndex, int pageSize, out int totalCount)
        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            int startRow = (pageIndex - 1) * pageSize + 1;
            int endRow = pageIndex * pageSize;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                keyword = null;
            }

            var list = new List<Product>();
            totalCount = 0;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                // 1) 先取得總筆數
                cmd.CommandText = @"
SELECT COUNT(*)
FROM dbo.Products
WHERE (@Keyword IS NULL
       OR ProductCode LIKE '%' + @Keyword + '%'
       OR Name       LIKE '%' + @Keyword + '%');";

                SqlParameter pKeyword = new SqlParameter("@Keyword", SqlDbType.NVarChar, 100);
                if (keyword == null)
                {
                    pKeyword.Value = DBNull.Value;
                }
                else
                {
                    pKeyword.Value = keyword;
                }
                cmd.Parameters.Add(pKeyword);

                conn.Open();
                object countObj = cmd.ExecuteScalar();
                if (countObj != null && countObj != DBNull.Value)
                {
                    totalCount = Convert.ToInt32(countObj);
                }

                // 如果沒有任何資料，直接回傳空集合
                if (totalCount == 0)
                {
                    return list;
                }

                // 2) 再取得分頁資料
                cmd.Parameters.Clear();
                cmd.CommandText = @"
;WITH ProductCTE AS
(
    SELECT  Id,
            ProductCode,
            Name,
            Unit,
            SafeStockQty,
            ROW_NUMBER() OVER (ORDER BY Id) AS RowNum
    FROM dbo.Products
    WHERE (@Keyword IS NULL
           OR ProductCode LIKE '%' + @Keyword + '%'
           OR Name       LIKE '%' + @Keyword + '%')
)
SELECT Id, ProductCode, Name, Unit, SafeStockQty
FROM ProductCTE
WHERE RowNum BETWEEN @StartRow AND @EndRow
ORDER BY RowNum;";

                // 重新設定參數
                pKeyword = new SqlParameter("@Keyword", SqlDbType.NVarChar, 100);
                if (keyword == null)
                {
                    pKeyword.Value = DBNull.Value;
                }
                else
                {
                    pKeyword.Value = keyword;
                }
                cmd.Parameters.Add(pKeyword);

                cmd.Parameters.Add("@StartRow", SqlDbType.Int).Value = startRow;
                cmd.Parameters.Add("@EndRow", SqlDbType.Int).Value = endRow;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product p = new Product();
                        int i = 0;
                        p.Id = reader.GetInt32(i++);
                        p.ProductCode = reader.GetString(i++);
                        p.Name = reader.GetString(i++);
                        p.Unit = reader.IsDBNull(i) ? null : reader.GetString(i);
                        i++;
                        p.SafeStockQty = reader.GetInt32(i++);

                        list.Add(p);
                    }
                }
            }

            return list;
        }
    }
}