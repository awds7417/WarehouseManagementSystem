using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WarehouseManagementSystem.Models
{
    public class InventoryReportRepository
    {
        private readonly string _connectionString;

        public InventoryReportRepository()
        {
            _connectionString = ConfigurationManager
                .ConnectionStrings["WarehouseDb"]
                .ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // 原本的 GetInventorySummary() 保留不動
        public IList<InventorySummary> GetInventorySummary()
        {
            var list = new List<InventorySummary>();

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
SELECT  p.Id,
        p.ProductCode,
        p.Name       AS ProductName,
        p.Unit,
        ISNULL(SUM(CASE WHEN t.TxType = 'IN'  THEN t.Quantity ELSE 0 END), 0) AS TotalIn,
        ISNULL(SUM(CASE WHEN t.TxType = 'OUT' THEN t.Quantity ELSE 0 END), 0) AS TotalOut,
        ISNULL(SUM(CASE WHEN t.TxType = 'IN'  THEN t.Quantity ELSE 0 END), 0) -
        ISNULL(SUM(CASE WHEN t.TxType = 'OUT' THEN t.Quantity ELSE 0 END), 0) AS CurrentStock
FROM dbo.Products p
LEFT JOIN dbo.InventoryTransactions t ON t.ProductId = p.Id
GROUP BY p.Id, p.ProductCode, p.Name, p.Unit
ORDER BY p.ProductCode;";

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        InventorySummary s = new InventorySummary();
                        int i = 0;
                        s.ProductId = reader.GetInt32(i++);
                        s.ProductCode = reader.GetString(i++);
                        s.ProductName = reader.GetString(i++);
                        s.Unit = reader.IsDBNull(i) ? null : reader.GetString(i++);
                        s.TotalIn = reader.GetInt32(i++);
                        s.TotalOut = reader.GetInt32(i++);
                        s.CurrentStock = reader.GetInt32(i++);

                        list.Add(s);
                    }
                }
            }

            return list;
        }

        // 🔽 新增：分頁版
        public IList<InventorySummary> GetInventorySummaryPaged(int pageIndex, int pageSize, out int totalCount)
        {
            if (pageIndex < 1)
                pageIndex = 1;

            if (pageSize <= 0)
                pageSize = 6; // 一頁 6 筆

            int startRow = (pageIndex - 1) * pageSize + 1;
            int endRow = pageIndex * pageSize;

            var list = new List<InventorySummary>();
            totalCount = 0;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                // 1) 先算總共有幾個產品（幾筆庫存紀錄）
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Products;";
                conn.Open();

                object countObj = cmd.ExecuteScalar();
                if (countObj != null && countObj != DBNull.Value)
                {
                    totalCount = Convert.ToInt32(countObj);
                }

                if (totalCount == 0)
                {
                    return list;
                }

                // 2) 再用 ROW_NUMBER 做分頁，抓本頁資料
                cmd.Parameters.Clear();
                cmd.CommandText = @"
;WITH SummaryCTE AS
(
    SELECT  p.Id AS ProductId,
            p.ProductCode,
            p.Name       AS ProductName,
            p.Unit,
            ISNULL(SUM(CASE WHEN t.TxType = 'IN'  THEN t.Quantity ELSE 0 END), 0) AS TotalIn,
            ISNULL(SUM(CASE WHEN t.TxType = 'OUT' THEN t.Quantity ELSE 0 END), 0) AS TotalOut,
            ISNULL(SUM(CASE WHEN t.TxType = 'IN'  THEN t.Quantity ELSE 0 END), 0) -
            ISNULL(SUM(CASE WHEN t.TxType = 'OUT' THEN t.Quantity ELSE 0 END), 0) AS CurrentStock,
            ROW_NUMBER() OVER (ORDER BY p.ProductCode) AS RowNum
    FROM dbo.Products p
    LEFT JOIN dbo.InventoryTransactions t ON t.ProductId = p.Id
    GROUP BY p.Id, p.ProductCode, p.Name, p.Unit
)
SELECT  ProductId,
        ProductCode,
        ProductName,
        Unit,
        TotalIn,
        TotalOut,
        CurrentStock
FROM SummaryCTE
WHERE RowNum BETWEEN @StartRow AND @EndRow
ORDER BY RowNum;";

                cmd.Parameters.Add("@StartRow", SqlDbType.Int).Value = startRow;
                cmd.Parameters.Add("@EndRow", SqlDbType.Int).Value = endRow;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        InventorySummary s = new InventorySummary();
                        int i = 0;

                        s.ProductId = reader.GetInt32(i++);
                        s.ProductCode = reader.GetString(i++);
                        s.ProductName = reader.GetString(i++);
                        s.Unit = reader.IsDBNull(i) ? null : reader.GetString(i++);
                        s.TotalIn = reader.GetInt32(i++);
                        s.TotalOut = reader.GetInt32(i++);
                        s.CurrentStock = reader.GetInt32(i++);

                        list.Add(s);
                    }
                }
            }

            return list;
        }
    }
}
