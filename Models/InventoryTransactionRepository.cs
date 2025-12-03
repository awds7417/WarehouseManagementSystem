using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WarehouseManagementSystem.Models
{
    public class InventoryTransactionRepository
    {
        private readonly string _connectionString;

        public InventoryTransactionRepository()
        {
            _connectionString = ConfigurationManager
                .ConnectionStrings["WarehouseDb"]
                .ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // 1) 取得全部進出貨紀錄（含產品/客戶名稱）
        public IList<InventoryTransaction> GetAll()
        {
            var list = new List<InventoryTransaction>();

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                SELECT  t.Id,
                        t.ProductId,
                        t.CustomerId,
                        t.TxType,
                        t.Quantity,
                        t.TxDate,
                        t.Remark,
                        p.ProductCode,
                        p.Name       AS ProductName,
                        c.CustomerCode,
                        c.Name       AS CustomerName
                FROM dbo.InventoryTransactions t
                JOIN dbo.Products p
                    ON t.ProductId = p.Id
                LEFT JOIN dbo.Customers c
                    ON t.CustomerId = c.Id
                ORDER BY t.TxDate DESC, t.Id DESC;";

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        InventoryTransaction tx = new InventoryTransaction();
                        int index = 0;

                        tx.Id = reader.GetInt32(index++);          // 0
                        tx.ProductId = reader.GetInt32(index++);   // 1

                        if (reader.IsDBNull(index))
                            tx.CustomerId = null;
                        else
                            tx.CustomerId = reader.GetInt32(index);
                        index++;

                        tx.TxType = reader.GetString(index++);     // 3
                        tx.Quantity = reader.GetInt32(index++);    // 4
                        tx.TxDate = reader.GetDateTime(index++);   // 5

                        tx.Remark = reader.IsDBNull(index) ? null : reader.GetString(index);
                        index++;

                        tx.ProductCode = reader.GetString(index++);         // 7
                        tx.ProductName = reader.GetString(index++);         // 8
                        tx.CustomerCode = reader.IsDBNull(index) ? null : reader.GetString(index);
                        index++;
                        tx.CustomerName = reader.IsDBNull(index) ? null : reader.GetString(index);
                        index++;

                        list.Add(tx);
                    }
                }
            }

            return list;
        }

        // 2) 取得單一筆（目前只用於刪除確認）
        public InventoryTransaction GetById(int id)
        {
            InventoryTransaction tx = null;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                SELECT  t.Id,
                        t.ProductId,
                        t.CustomerId,
                        t.TxType,
                        t.Quantity,
                        t.TxDate,
                        t.Remark,
                        p.ProductCode,
                        p.Name       AS ProductName,
                        c.CustomerCode,
                        c.Name       AS CustomerName
                FROM dbo.InventoryTransactions t
                JOIN dbo.Products p
                    ON t.ProductId = p.Id
                LEFT JOIN dbo.Customers c
                    ON t.CustomerId = c.Id
                WHERE t.Id = @Id;";

                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tx = new InventoryTransaction();
                        int index = 0;

                        tx.Id = reader.GetInt32(index++);
                        tx.ProductId = reader.GetInt32(index++);

                        if (reader.IsDBNull(index))
                            tx.CustomerId = null;
                        else
                            tx.CustomerId = reader.GetInt32(index);
                        index++;

                        tx.TxType = reader.GetString(index++);
                        tx.Quantity = reader.GetInt32(index++);
                        tx.TxDate = reader.GetDateTime(index++);

                        tx.Remark = reader.IsDBNull(index) ? null : reader.GetString(index);
                        index++;

                        tx.ProductCode = reader.GetString(index++);
                        tx.ProductName = reader.GetString(index++);
                        tx.CustomerCode = reader.IsDBNull(index) ? null : reader.GetString(index);
                        index++;
                        tx.CustomerName = reader.IsDBNull(index) ? null : reader.GetString(index);
                        index++;
                    }
                }
            }

            return tx;
        }

        //進出貨的 Transaction + Lock（防止負庫存 / 併發問題）
        public void Insert(InventoryTransaction tx)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();

                using (SqlTransaction tran = conn.BeginTransaction())
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;

                    // 1) 先算目前庫存，並且鎖住這個產品的相關交易列
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                    SELECT ISNULL(SUM(
                               CASE WHEN TxType = 'IN'  THEN Quantity
                                    WHEN TxType = 'OUT' THEN -Quantity
                                    ELSE 0 END
                           ), 0) AS CurrentStock
                    FROM dbo.InventoryTransactions WITH (UPDLOCK, HOLDLOCK)
                    WHERE ProductId = @ProductId;";

                    cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = tx.ProductId;

                    int currentStock = (int)cmd.ExecuteScalar();

                    // 2) 如果是出庫，要檢查庫存是否足夠
                    if (tx.TxType == "OUT" && currentStock < tx.Quantity)
                    {
                        tran.Rollback();
                        throw new InvalidOperationException(
                            "庫存不足（目前庫存 = " + currentStock.ToString() +
                            "，出庫數量 = " + tx.Quantity.ToString() + "），無法出庫。");
                    }

                    // 3) 通過檢查後，才真正寫入進出貨紀錄
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                    INSERT INTO dbo.InventoryTransactions
                        (ProductId, CustomerId, TxType, Quantity, TxDate, Remark)
                    VALUES
                        (@ProductId, @CustomerId, @TxType, @Quantity, @TxDate, @Remark);";

                    cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = tx.ProductId;

                    if (tx.CustomerId.HasValue)
                        cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = tx.CustomerId.Value;
                    else
                        cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = DBNull.Value;

                    cmd.Parameters.Add("@TxType", SqlDbType.NVarChar, 3).Value = tx.TxType;
                    cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = tx.Quantity;
                    cmd.Parameters.Add("@TxDate", SqlDbType.DateTime).Value = tx.TxDate;

                    if (tx.Remark == null)
                        cmd.Parameters.Add("@Remark", SqlDbType.NVarChar, 200).Value = DBNull.Value;
                    else
                        cmd.Parameters.Add("@Remark", SqlDbType.NVarChar, 200).Value = tx.Remark;

                    cmd.ExecuteNonQuery();

                    // 4) 一切成功才 Commit
                    tran.Commit();
                }
            }
        }


        // 4) 刪除進出貨紀錄（實務上通常不用改內容，只允許刪除）
        public void Delete(int id)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "DELETE FROM dbo.InventoryTransactions WHERE Id = @Id;";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 5)分頁
        public IList<InventoryTransaction> GetPaged(int pageIndex, int pageSize, out int totalCount)
        {
            if (pageIndex < 1)
                pageIndex = 1;

            if (pageSize <= 0)
                pageSize = 6;  // 一頁 6 筆

            int startRow = (pageIndex - 1) * pageSize + 1;
            int endRow = pageIndex * pageSize;

            var list = new List<InventoryTransaction>();
            totalCount = 0;

            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                // 1) 先算總筆數
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.InventoryTransactions;";
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

                // 2) 再抓本頁資料（包一層 CTE + ROW_NUMBER）
                cmd.Parameters.Clear();
                cmd.CommandText = @"
                ;WITH TxCTE AS
                (
                    SELECT  t.Id,
                            t.ProductId,
                            t.CustomerId,
                            t.TxType,
                            t.Quantity,
                            t.TxDate,
                            t.Remark,
                            p.ProductCode,
                            p.Name       AS ProductName,
                            c.Name       AS CustomerName,
                            ROW_NUMBER() OVER (ORDER BY t.TxDate DESC, t.Id DESC) AS RowNum
                    FROM dbo.InventoryTransactions t
                    JOIN dbo.Products p       ON t.ProductId = p.Id
                    LEFT JOIN dbo.Customers c ON t.CustomerId = c.Id
                )
                SELECT  Id,
                        ProductId,
                        CustomerId,
                        TxType,
                        Quantity,
                        TxDate,
                        Remark,
                        ProductCode,
                        ProductName,
                        CustomerName
                FROM TxCTE
                WHERE RowNum BETWEEN @StartRow AND @EndRow
                ORDER BY RowNum;";

                cmd.Parameters.Add("@StartRow", SqlDbType.Int).Value = startRow;
                cmd.Parameters.Add("@EndRow", SqlDbType.Int).Value = endRow;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        InventoryTransaction tx = new InventoryTransaction();
                        int i = 0;

                        tx.Id = reader.GetInt32(i++);
                        tx.ProductId = reader.GetInt32(i++);

                        if (reader.IsDBNull(i))
                            tx.CustomerId = null;
                        else
                            tx.CustomerId = reader.GetInt32(i);
                        i++;

                        tx.TxType = reader.GetString(i++);
                        tx.Quantity = reader.GetInt32(i++);
                        tx.TxDate = reader.GetDateTime(i++);
                        tx.Remark = reader.IsDBNull(i) ? null : reader.GetString(i++);

                        // 如果你在 InventoryTransaction 類別有這些欄位，就塞進去
                        // 如果沒有，就先加在 Model 裡，或暫時拿掉這幾行
                        // tx.ProductCode = reader.GetString(i++);
                        // tx.ProductName = reader.GetString(i++);
                        // tx.CustomerName = reader.IsDBNull(i) ? null : reader.GetString(i++);

                        list.Add(tx);
                    }
                }
            }

            return list;
        }
    }
}
